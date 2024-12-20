﻿using Microsoft.Win32;
using Sim80C51.Common;
using Sim80C51.Toolbox;
using Sim80C51.Toolbox.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using YamlDotNet.Serialization;

namespace Sim80C51
{
    public class SimulatorWindowContext : NotifyPropertyChanged
    {
        #region Private Properties
        private SimulatorWindow? owner;
        private Interfaces.I80C51? CPU;
        private Controls.ListingEditorContext? listingCtx;
        private readonly SortedDictionary<ushort, Controls.MemoryContext> xmemCtx = [];
        private readonly DispatcherTimer stepTimer = new();
        #endregion

        #region Workspace commands
        public ICommand LoadWorkspaceCommand => new RelayCommand((o) =>
        {
            if (ProcessorActivated)
            {
                return;
            }

            OpenFileDialog openFileDialog = new()
            {
                DefaultExt = "s80c51.yml",
                Filter = "Workspace Files (*.s80c51.yml)|*.s80c51.yml",
                Title = "Load Workspace",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog(owner) == false)
            {
                return;
            }

            using StreamReader reader = File.OpenText(openFileDialog.FileName);
            IDeserializer deserializer = new DeserializerBuilder().WithTypeMapping<Interfaces.ICallStackEntry, Processors.CallStackEntry>().Build();
            WSpace.Workspace wsp = deserializer.Deserialize<WSpace.Workspace>(reader);

            SelectedProcessor = ProcessorList.FirstOrDefault(p => p.Key == wsp.ProcessorType).Value;
            if (SelectedProcessor == null)
            {
                return;
            }

            ProcessorActivated = true;
            InitCPU();

            if (listingCtx == null || CPU == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(wsp.InternalMemory))
            {
                Processors.ByteRow[] rows = LoadByteRowsFromCompBase64(wsp.InternalMemory);
                for (int i = 0; i < rows.Length && i < CPU.CoreMemory.Count; i++)
                {
                    CPU.CoreMemory[i].UpdateFromBuffer([.. rows[i].Row]);
                }
                CPU.RefreshUIProperies();
            }

            if (!string.IsNullOrEmpty(wsp.Listing))
            {
                listingCtx.SetProcessorType(SelectedProcessor);
                SetListingFromCompBase64(wsp.Listing);
            }

            CPU.PC = wsp.ProgramCounter;
            listingCtx.HighlightAddress = CPU.PC;

            Breakpoints.Clear();
            foreach (ushort address in wsp.Breakpoints)
            {
                Breakpoints.Add(address);
            }

            MemoryPointer.Clear();
            foreach (KeyValuePair<ushort, string> mw in wsp.MemoryWatches)
            {
                MemoryPointer.Add(mw.Key, (MemoryAccess)mw.Value);
            }

            CPU.CallStack.Clear();
            foreach (Interfaces.ICallStackEntry cs in wsp.CallStack)
            {
                CPU.CallStack.Add(cs);
            }

            foreach (ushort address in wsp.XMem.Keys)
            {
                xmemCtx.Add(address, LoadXMemFromCompBase64(wsp.XMem[address].Memory));
                if (wsp.XMem[address].M48TEnabled)
                {
                    xmemCtx[address].StartM48TMode();
                }

                AddXMemTab(address, $"XMem at 0x{address:X4} size 0x{xmemCtx[address].Size:X4}");
            }

            CPU.LoadAdditionalSettings(wsp.AdditionalSettings);
        });
        public ICommand SaveWorkspaceCommand => new RelayCommand((o) =>
        {
            if (listingCtx == null || CPU == null || SelectedProcessor == null)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                DefaultExt = "s80c51.yml",
                Filter = "Workspace Files (*.s80c51.yml)|*.s80c51.yml",
                Title = "Save Workspace",
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog(owner) == false)
            {
                return;
            }

            WSpace.Workspace wsp = new()
            {
                ProgramCounter = CPU.PC,
                InternalMemory = DataToBase64(CPU.CoreMemory)
            };
            if (SelectedProcessor.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute nameAttr)
            {
                wsp.ProcessorType = nameAttr.DisplayName;
            }

            foreach (ushort address in xmemCtx.Keys)
            {
                wsp.XMem.Add(address, new WSpace.XMemConfig()
                {
                    Memory = DataToBase64(xmemCtx[address].Memory!),
                    M48TEnabled = xmemCtx[address].M48TMode
                });
            }

            using (MemoryStream ms = new())
            {
                listingCtx.SaveListingToStream(ms, true);
                ms.Position = 0;
                wsp.Listing = StreamToCompressedBase64(ms);
            }

            wsp.Breakpoints.AddRange(Breakpoints);
            foreach (KeyValuePair<ushort, MemoryAccess> mw in MemoryPointer)
            {
                wsp.MemoryWatches.Add(mw.Key, mw.Value);
            }
            wsp.CallStack.AddRange(CPU.CallStack);
            CPU.SaveAdditionalSettings(wsp.AdditionalSettings);
            using StreamWriter writer = File.CreateText(saveFileDialog.FileName);
            Serializer serializer = new();
            serializer.Serialize(writer, wsp);
        });
        public static ICommand ExitCommand => new RelayCommand((o) => Application.Current.Shutdown());
        public ICommand ActivateDeviceConfigCommand => new RelayCommand((o) =>
        {
            ProcessorActivated = true;
            InitCPU();
        });

        public ICommand ListingLoadCommand => new RelayCommand((o) =>
        {
            if (SelectedProcessor == null || listingCtx == null)
            {
                return;
            }

            OpenFileDialog openFileDialog = new()
            {
                DefaultExt = "l51",
                Filter = "Listing File or Binary (*.l51,*.bin,*.hex,*.h51)|*.l51;*.bin;*.hex;*.h51|Listing File (*.l51)|*.l51|Binary File (*.bin)|*.bin|Intel HEX File (*.hex;*.h51)|*.hex;*.h51",
                Title = "Load Listing or ROM Binary",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog(owner) == false)
            {
                return;
            }

            listingCtx.SetProcessorType(SelectedProcessor);
            string ext = Path.GetExtension(openFileDialog.FileName);
            switch (ext)
            {
                case ".l51":
                    listingCtx.LoadFromListingStream(File.OpenRead(openFileDialog.FileName));
                    break;
                case ".bin":
                    listingCtx.LoadRomBinaryFile(openFileDialog.FileName);
                    break;
                case ".hex":
                case ".h51":
                    listingCtx.LoadRomHexFile(openFileDialog.FileName);
                    break;
            }
        });
        public ICommand ListingSaveCommand => new RelayCommand((o) =>
        {
            if (listingCtx == null)
            {
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                DefaultExt = "l51",
                Filter = "Listing Files (*.l51)|*.l51",
                Title = "Save Listing",
                CheckPathExists = true,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog(owner) == false)
            {
                return;
            }

            listingCtx.SaveListingToStream(File.OpenWrite(saveFileDialog.FileName));
        });

        public ICommand AddExternalMemoryCommand => new RelayCommand((o) =>
        {
            if (owner == null)
            {
                return;
            }

            ushort start = 0;
            if (xmemCtx.Count > 0)
            {
                start = xmemCtx.Keys.Last();
                start += (ushort)xmemCtx[start].Size;
            }

            SelectRamWindow wnd = new()
            {
                Owner = owner,
                RamStartAddress = start
            };

            if (wnd.ShowDialog() == false)
            {
                return;
            }

            if (xmemCtx.ContainsKey(wnd.RamStartAddress))
            {
                MessageBox.Show("Error", $"External Memory at address {wnd.RamStartAddress:X4} exists!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int size = wnd.RamSize * 1024;

            xmemCtx.Add(wnd.RamStartAddress, new()
            {
                Memory = new(Processors.ByteRow.InitRows(size, wnd.EnableM48T)),
                MarkUpperInternalRam = false,
                Size = size
            });

            if (wnd.EnableM48T)
            {
                xmemCtx[wnd.RamStartAddress].StartM48TMode();
            }

            AddXMemTab(wnd.RamStartAddress, $"XMem at 0x{wnd.RamStartAddress:X4} size 0x{size:X4}");
        });
        public ICommand LoadExternalMemoryCommand => new RelayCommand((o) =>
        {
            if (owner == null)
            {
                return;
            }

            OpenFileDialog openFileDialog = new()
            {
                DefaultExt = "bin",
                Filter = "Binary Files (*.bin)|*.bin",
                CheckFileExists = true,
                Title = "Load External Memory"
            };

            if (openFileDialog.ShowDialog(owner) == false)
            {
                return;
            }

            ushort start = 0;
            if (xmemCtx.Count > 0)
            {
                start = xmemCtx.Keys.Last();
                start += (ushort)xmemCtx[start].Size;
            }

            SelectRamWindow wnd = new()
            {
                Owner = owner,
                RamStartAddress = start,
                HideSize = true,
            };

            if (wnd.ShowDialog() == false)
            {
                return;
            }

            if (xmemCtx.ContainsKey(wnd.RamStartAddress))
            {
                MessageBox.Show("Error", $"External Memory at address {wnd.RamStartAddress:X4} exists!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using FileStream file = File.OpenRead(openFileDialog.FileName);
            xmemCtx.Add(wnd.RamStartAddress, new()
            {
                Memory = new(Processors.ByteRow.FromStream(file)),
                MarkUpperInternalRam = false,
                Size = file.Length
            });

            if (wnd.EnableM48T)
            {
                xmemCtx[wnd.RamStartAddress].StartM48TMode();
            }

            AddXMemTab(wnd.RamStartAddress, $"XMem at 0x{wnd.RamStartAddress:X4} size 0x{file.Length:X4}");
        });

        public ICommand CheckOverlapCommand => new RelayCommand((o) =>
        {
            listingCtx?.CheckOverlap();
        });
        #endregion

        #region Simulator commands
        public ICommand OneStepCommand => new RelayCommand((o) =>
        {
            if (listingCtx?.GetFromAddress(CPU!.PC) is ListingEntry entry)
            {
                CPU.Process(entry);
                listingCtx.HighlightAddress = CPU.PC;
                GotoPcCommand?.Execute(null);
            }
        });

        public ICommand PlayCommand => new RelayCommand((o) =>
        {
            CPU!.UiUpdates = true;
            stepTimer.Start();
            (PlayCommand as RelayCommand)?.OnCanExecuteChanged();
            (PlayHiddenCommand as RelayCommand)?.OnCanExecuteChanged();
            (PauseCommand as RelayCommand)?.OnCanExecuteChanged();
        }, (o) => { return !stepTimer.IsEnabled; });

        public ICommand PlayHiddenCommand => new RelayCommand((o) =>
        {
            CPU!.UiUpdates = false;
            stepTimer.Start();
            (PlayCommand as RelayCommand)?.OnCanExecuteChanged();
            (PlayHiddenCommand as RelayCommand)?.OnCanExecuteChanged();
            (PauseCommand as RelayCommand)?.OnCanExecuteChanged();
        }, (o) => { return !stepTimer.IsEnabled; });

        public ICommand PauseCommand => new RelayCommand((o) =>
        {
            StopStepTimer();
        }, (o) => { return stepTimer.IsEnabled; });

        public ICommand ResetCommand => new RelayCommand((o) =>
        {
            if (stepTimer.IsEnabled)
            {
                StopStepTimer();
            }
            CPU?.Reset();
            GotoPcCommand?.Execute(null);
        });
        #endregion

        #region Navigate Commands
        public ICommand GotoPcCommand => new RelayCommand((o) =>
        {
            if (listingCtx?.GetFromAddress(CPU!.PC) is ListingEntry entry)
            {
                listingCtx.SelectedListingEntry = entry;
            }
        });

        public ICommand GotoDptrCommand => new RelayCommand((o) =>
        {
            if (listingCtx?.GetFromAddress(CPU!.DPTR) is ListingEntry entry)
            {
                listingCtx.SelectedListingEntry = entry;
            }
        });        

        public ICommand NavToAddressCommand => new RelayCommand((o) =>
        {
            if (o is ushort address && listingCtx?.GetFromAddress(address) is ListingEntry entry)
            {
                listingCtx.SelectedListingEntry = entry;
            }
            else if (o is Processors.CallStackEntry csEntry && listingCtx?.GetFromAddress(csEntry.Address) is ListingEntry listingEntry)
            {
                listingCtx.SelectedListingEntry = listingEntry;
            }
        });

        public ICommand DeleteBpCommand => new RelayCommand((o) =>
        {
            if (o is ushort address && Breakpoints.Contains(address))
            {
                Breakpoints.Remove(address);
            }
        });

        public ICommand AddDptrCommand => new RelayCommand((o) =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(DptrAddValue, @"\A\b[0-9a-fA-F]+\b\Z") && o is string access)
            {
                ushort value = Convert.ToUInt16(DptrAddValue, 16);
                if (!MemoryPointer.ContainsKey(value))
                {
                    MemoryPointer.Add(value, (MemoryAccess)access);
                }
                DptrAddValue = string.Empty;
            }
        });

        public ICommand DeleteDptrCommand => new RelayCommand((o) =>
        {
            if (o is ushort address && MemoryPointer.ContainsKey(address))
            {
                MemoryPointer.Remove(address);
            }
        });

        #endregion

        #region Hardware Commands
        Interfaces.IHardwareWindow? hardwareWindow = null;
        public ICommand EnableTanningBedCommand => new RelayCommand((o) =>
        {
            hardwareWindow ??= new TanningBed.MainWindow()
            {
                Owner = owner,
                Model =
                {
                    CPU = CPU as Interfaces.IP80C552
                }
            };            
            hardwareWindow.Show();
        }, (o) => (hardwareWindow == null || hardwareWindow is TanningBed.MainWindow) && CPU is Interfaces.IP80C552);
        #endregion

        #region Property Bindings
        public Dictionary<string, Type> ProcessorList { get; } = [];

        public Type? SelectedProcessor { get => selectedProcessor; set { selectedProcessor = value; DoPropertyChanged(); } }
        private Type? selectedProcessor;

        public bool ProcessorActivated { get => processorActivated; set { processorActivated = value; DoPropertyChanged(); } }
        private bool processorActivated = false;

        public ObservableCollection<ushort> Breakpoints { get; } = [];

        public ObservableDictionary<ushort, MemoryAccess> MemoryPointer { get; } = [];

        public string DptrAddValue { get => dptrAddValue; set { dptrAddValue = value; DoPropertyChanged(); } }
        private string dptrAddValue = string.Empty;

        public ICollectionView? LabelView { get => labelView; set { labelView = value; DoPropertyChanged(); } }
        private ICollectionView? labelView;

        public ObservableCollection<Interfaces.ICallStackEntry>? CallStack => CPU?.CallStack;
        #endregion

        #region Init functions
        public SimulatorWindowContext()
        {
            LoadDeviceList();
        }

        private void LoadDeviceList()
        {
            Type cpuInterfaceType = typeof(Interfaces.I80C51);
            IEnumerable<Type> cpuTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes().Where(t => t.GetInterfaces().Contains(cpuInterfaceType)));
            foreach (Type procType in cpuTypes)
            {
                if (procType.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute nameAttr)
                {
                    ProcessorList.Add(nameAttr.DisplayName, procType);
                }
            }
        }

        public void Loaded(SimulatorWindow simulatorWindow)
        {
            owner = simulatorWindow;
            listingCtx = simulatorWindow.listingEditor.DataContext as Controls.ListingEditorContext;
            listingCtx!.AddBreakPoint = AddBreakPoint;
            stepTimer.Tick += new EventHandler(StepTimer_Tick);
            stepTimer.Interval = TimeSpan.FromTicks(1);
            LabelView = new CollectionViewSource() { Source = listingCtx.Listing }.View;
            LabelView.Filter = (entry) => !string.IsNullOrEmpty((entry as ListingEntry)?.Label);
        }
        #endregion

        private void StopStepTimer()
        {
            if (stepTimer.IsEnabled)
            {
                stepTimer.Stop();
                (PlayCommand as RelayCommand)?.OnCanExecuteChanged();
                (PlayHiddenCommand as RelayCommand)?.OnCanExecuteChanged();
                (PauseCommand as RelayCommand)?.OnCanExecuteChanged();
                CPU!.UiUpdates = true;
                CPU!.RefreshUIProperies();
                GotoPcCommand.Execute(null);
            }
        }

        private void StepTimer_Tick(object? sender, EventArgs e)
        {
            int loopStepCounter = 0;
            do
            {
                if (listingCtx?.GetFromAddress(CPU!.PC) is not ListingEntry entry || entry.Instruction == Processors.InstructionType.DB)
                {
                    StopStepTimer();
                    return;
                }

                CPU.Process(entry);
                listingCtx.HighlightAddress = CPU.PC;

                if (Breakpoints.Contains(CPU.PC))
                {
                    StopStepTimer();
                    return;
                }
                loopStepCounter++;
            } while (stepTimer.IsEnabled && loopStepCounter < 20);
        }

        private void AddBreakPoint(ushort address)
        {
            if (Breakpoints.Contains(address))
            {
                return;
            }

            for (int i = 0; i < Breakpoints.Count; i++)
            {
                if (Breakpoints[i] > address)
                {
                    Breakpoints.Insert(i, address);
                    return;
                }
            }
            Breakpoints.Add(address);
        }

        private void AddXMemTab(ushort ramStartAddress, string title)
        {
            if (owner == null)
            {
                return;
            }

            TabItem tItem = new()
            {
                Header = title,
                Content = new Controls.Memory()
                {
                    DataContext = xmemCtx[ramStartAddress]
                }
            };

            owner.memBox.Items.Add(tItem);
            owner.memBox.SelectedItem = tItem;
        }

        private void InitCPU()
        {
            if (SelectedProcessor == null)
            {
                return;
            }

            Controls.CPU.ICPUControl? cpu = SelectedProcessor.Name switch
            {
                nameof(Processors.P80C552) => new Controls.CPU.P80C552(),
                _ => throw new NotImplementedException("Selected Processor not supported!"),
            };

            if (cpu == null || cpu.CPUContext == null || owner == null)
            {
                return;
            }

            CPU = cpu.CPUContext;
            CPU.SetRamByte = SetRamByte;
            CPU.GetRamByte = GetRamByte;
            CPU.GetCodeByte = GetCodeByte;

            owner.cpuBox.Content = (UIElement)cpu;
            owner.cpuBox.Visibility = Visibility.Visible;

            owner.intMemBox.Children.Add(new Controls.Memory()
            {
                DataContext = new Controls.MemoryContext()
                {
                    Memory = CPU.CoreMemory,
                    MarkUpperInternalRam = true,
                }
            });

            DoPropertyChanged(nameof(CallStack));
        }

        private static string DataToBase64(ObservableCollection<Interfaces.IByteRow> data)
        {
            return StreamToCompressedBase64(Processors.ByteRow.ToMemoryStream(data));
        }

        private static string StreamToCompressedBase64(Stream stream)
        {
            string result = string.Empty;
            using (MemoryStream ms = new())
            {
                using (GZipStream compressor = new(ms, CompressionMode.Compress, true))
                {
                    stream.CopyTo(compressor);
                }

                ms.Position = 0;
                byte[] buffer = new byte[63];
                int count;
                while ((count = ms.Read(buffer, 0, buffer.Length)) > 0)
                {
                    result += Convert.ToBase64String(buffer, 0, count) + Environment.NewLine;
                }
            }
            return result;
        }

        private static Processors.ByteRow[] LoadByteRowsFromCompBase64(string mem)
        {
            using MemoryStream msComp = new();
            using (StringReader sr = new(mem))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    msComp.Write(Convert.FromBase64String(line));
                }
            }
            msComp.Position = 0;
            using GZipStream decomp = new(msComp, CompressionMode.Decompress);
            return Processors.ByteRow.FromStream(decomp);
        }

        private void SetListingFromCompBase64(string mem)
        {
            using MemoryStream msComp = new();
            using (StringReader sr = new(mem))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    msComp.Write(Convert.FromBase64String(line));
                }
            }
            msComp.Position = 0;
            using GZipStream decomp = new(msComp, CompressionMode.Decompress);
            listingCtx!.LoadFromListingStream(decomp);
        }

        private static Controls.MemoryContext LoadXMemFromCompBase64(string mem)
        {
            Processors.ByteRow[] rows = LoadByteRowsFromCompBase64(mem);
            int size = rows.Length * Processors.ByteRow.ROW_WIDTH;

            return new()
            {
                Memory = new(rows),
                MarkUpperInternalRam = false,
                Size = size
            };
        }

        #region simulator ram callbacks
        private void SetRamByte(ushort address, byte value)
        {
            if (xmemCtx.Count == 0)
            {
                return;
            }

            ushort lastMemAddress = xmemCtx.Keys.Where(a => a <= address).Last();
            if (xmemCtx[lastMemAddress].Size > address - lastMemAddress)
            {
                xmemCtx[lastMemAddress][address - lastMemAddress] = value;
            }
            else
            {
                StopStepTimer();
            }

            if (stepTimer.IsEnabled && MemoryPointer.TryGetValue(address, out MemoryAccess? access) && access.Write)
            {
                StopStepTimer();
            }
        }

        private byte GetRamByte(ushort address)
        {
            if (xmemCtx.Count == 0)
            {
                return 0xff;
            }

            if (stepTimer.IsEnabled && MemoryPointer.TryGetValue(address, out MemoryAccess? access) && access.Read)
            {
                StopStepTimer();
            }

            ushort lastMemAddress = xmemCtx.Keys.Where(a => a <= address).Last();            
            if (xmemCtx[lastMemAddress].Size <= address - lastMemAddress)
            {
                StopStepTimer();
                return 0xff;
            }
            return xmemCtx[lastMemAddress][address - lastMemAddress];
        }

        private byte GetCodeByte(ushort address)
        {
            if (stepTimer.IsEnabled && MemoryPointer.TryGetValue(address, out MemoryAccess? access) && access.Read)
            {
                StopStepTimer();
            }

            return listingCtx!.GetCodeByte(address);
        }
        #endregion
    }
}
