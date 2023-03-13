using Microsoft.Win32;
using Sim80C51.Common;
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
        private Processors.I80C51? CPU;
        private Controls.ListingEditorContext? listingCtx;
        private readonly SortedDictionary<ushort, Controls.MemoryContext> xmemCtx = new();
        private readonly DispatcherTimer stepTimer = new();
        private ushort lastDptr = 0;
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
            Deserializer deserializer = new();
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
                ByteRow[] rows = LoadByteRowsFromCompBase64(wsp.InternalMemory);
                for (int i = 0; i < rows.Length && i < CPU.CoreMemory.Count; i++)
                {
                    CPU.CoreMemory[i].UpdateFromBuffer(rows[i].Row.ToArray());
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
            foreach (ushort address in wsp.MemoryWatches)
            {
                MemoryPointer.Add(address);
            }

            CPU.CallStack.Clear();
            foreach (ushort address in wsp.CallStack)
            {
                CPU.CallStack.Add(address);
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
            wsp.MemoryWatches.AddRange(MemoryPointer);
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
                Memory = new(ByteRow.InitRows(size, wnd.EnableM48T)),
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
                Memory = new(ByteRow.FromStream(file)),
                MarkUpperInternalRam = false,
                Size = file.Length
            });

            if (wnd.EnableM48T)
            {
                xmemCtx[wnd.RamStartAddress].StartM48TMode();
            }

            AddXMemTab(wnd.RamStartAddress, $"XMem at 0x{wnd.RamStartAddress:X4} size 0x{file.Length:X4}");
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
            if (stepTimer.IsEnabled)
            {
                stepTimer.Stop();
            }
            else
            {
                stepTimer.Start();
            }
        });

        public ICommand ExecIRQCommand => new RelayCommand((o) =>
        {
            if (o is not Controls.IRQMenuItem irqItem || CPU == null)
            {
                return;
            }
            irqItem.Method!.Invoke(CPU, null);
        });

        public ICommand ResetCommand => new RelayCommand((o) => { CPU?.Reset(); GotoPcCommand?.Execute(null); });
        #endregion

        #region Navigate Commands
        public ICommand GotoPcCommand => new RelayCommand((o) =>
        {
            if (listingCtx?.GetFromAddress(CPU!.PC) is ListingEntry entry)
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
            if (System.Text.RegularExpressions.Regex.IsMatch(DptrAddValue, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                ushort value = Convert.ToUInt16(DptrAddValue, 16);
                if (!MemoryPointer.Contains(value))
                {
                    MemoryPointer.Add(value);
                }
                DptrAddValue = string.Empty;
            }
        });

        public ICommand DeleteDptrCommand => new RelayCommand((o) =>
        {
            if (o is ushort address && MemoryPointer.Contains(address))
            {
                MemoryPointer.Remove(address);
            }
        });       

        #endregion

        #region Property Bindings
        public Dictionary<string, Type> ProcessorList { get; } = new();

        public Type? SelectedProcessor { get => selectedProcessor; set { selectedProcessor = value; DoPropertyChanged(); } }
        private Type? selectedProcessor;

        public bool ProcessorActivated { get => processorActivated; set { processorActivated = value; DoPropertyChanged(); } }
        private bool processorActivated = false;

        public ObservableCollection<ushort> Breakpoints { get; } = new();

        public ObservableCollection<ushort> MemoryPointer { get; } = new();

        public string DptrAddValue { get => dptrAddValue; set { dptrAddValue = value; DoPropertyChanged(); } }
        private string dptrAddValue = string.Empty;

        public ICollectionView? LabelView { get => labelView; set { labelView = value; DoPropertyChanged(); } }
        private ICollectionView? labelView;

        public ObservableCollection<Controls.IRQMenuItem> IRQMenuItems { get; } = new ();

        public ObservableCollection<ushort>? CallStack => CPU?.CallStack;
        #endregion

        #region Init functions
        public SimulatorWindowContext()
        {
            LoadDeviceList();
        }

        private void LoadDeviceList()
        {
            foreach (Type procType in Assembly.GetExecutingAssembly().GetTypes().Where(mytype => mytype.GetInterfaces().Contains(typeof(Processors.I80C51))))
            {
                if (procType.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute nameAttr)
                {
                    ProcessorList.Add(nameAttr.DisplayName, procType);
                }
            }

            /*
            string filepath = Path.Combine(AppContext.BaseDirectory, "device");
            DirectoryInfo deviceDataDir = new(filepath);
            IDeserializer serializer = new DeserializerBuilder().Build();

            foreach (FileInfo file in deviceDataDir.GetFiles("*.yml"))
            {
                deviceConfigList.Add(serializer.Deserialize<DeviceConfig>(File.ReadAllText(Path.Combine(filepath, file.Name))));
            }*/
        }

        public void Loaded(SimulatorWindow simulatorWindow)
        {
            owner = simulatorWindow;
            listingCtx = simulatorWindow.listingEditor.DataContext as Controls.ListingEditorContext;
            listingCtx!.AddBreakPoint = AddBreakPoint;
            stepTimer.Tick += new EventHandler(StepTimer_Tick);
            stepTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            LabelView = new CollectionViewSource() { Source = listingCtx.Listing }.View;
            LabelView.Filter = (entry) => !string.IsNullOrEmpty((entry as ListingEntry)?.Label);
        }
        #endregion

        private void StepTimer_Tick(object? sender, EventArgs e)
        {
            if (listingCtx?.GetFromAddress(CPU!.PC) is not ListingEntry entry || entry.Instruction == InstructionType.DB)
            {
                stepTimer.Stop();
                return;
            }

            CPU.Process(entry);
            listingCtx.HighlightAddress = CPU.PC;

            if (Breakpoints.Contains(CPU.PC))
            {
                stepTimer.Stop();
                return;
            }

            if (lastDptr != CPU.DPTR)
            {
                lastDptr = CPU.DPTR;
                if (MemoryPointer.Contains(lastDptr))
                {
                    stepTimer.Stop();
                    return;
                }
            }
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
            CPU.GetCodeByte = listingCtx!.GetCodeByte;

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

            IEnumerable<MethodInfo> list = CPU.GetType().GetMethods().Where(m => m.GetCustomAttributes<Processors.IVAttribute>().Any());
            foreach (MethodInfo mi in list)
            {
                byte priority = mi.GetCustomAttribute<Processors.IVAttribute>()?.Priority ?? 0;
                string title = mi.Name["Interrupt_".Length..];

                InsertIRQSorted(new()
                {
                    Title = $"{title} ({priority})",
                    Method = mi,
                    Priority = priority
                });
            }

            DoPropertyChanged(nameof(CallStack));
        }

        private void InsertIRQSorted(Controls.IRQMenuItem iItem)
        {
            for (int i = 0; i < IRQMenuItems.Count; i++)
            {
                if (iItem.Priority < IRQMenuItems[i].Priority)
                {
                    IRQMenuItems.Insert(i, iItem);
                    return;
                }
            }
            IRQMenuItems.Add(iItem);
        }

        private static string DataToBase64(ObservableCollection<ByteRow> data)
        {
            return StreamToCompressedBase64(ByteRow.ToMemoryStream(data));
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

        private static ByteRow[] LoadByteRowsFromCompBase64(string mem)
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
            return ByteRow.FromStream(decomp);
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
            ByteRow[] rows = LoadByteRowsFromCompBase64(mem);
            int size = rows.Length * ByteRow.ROW_WIDTH;

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
            xmemCtx[lastMemAddress][address - lastMemAddress] = value;
        }

        private byte GetRamByte(ushort address)
        {
            if (xmemCtx.Count == 0)
            {
                return 0xff;
            }

            ushort lastMemAddress = xmemCtx.Keys.Where(a => a <= address).Last();
            return xmemCtx[lastMemAddress][address - lastMemAddress];
        }
        #endregion
    }
}
