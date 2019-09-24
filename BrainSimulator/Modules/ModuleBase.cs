﻿//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Xml.Serialization;


namespace BrainSimulator.Modules
{
    abstract public class ModuleBase
    {
        protected NeuronArray theNeuronArray { get => MainWindow.theNeuronArray; }
        protected ModuleView na = null;
        protected ModuleView naIn = null;
        protected ModuleView naOut = null;

        //this is public so it will be included in the saved xml file.  That way
        //initialized data content can be preserved from run to run and only reinitialized when requested.
        public bool initialized = false;

        protected int minWidth = 2;
        protected int minHeight = 2;
        public int MinWidth => minWidth;
        public int MinHeight => minHeight;


        protected ModuleBaseDlg dlg = null;
        public Point dlgPos;
        public Point dlgSize;
        public bool dlgIsOpen = false;
        public virtual string ShortDescription { get; }
        public virtual string LongDescription { get; }


        public ModuleBase() { }

        abstract public void Fire();

        virtual public void Initialize()
        { }

        public void Init(bool forceInit = false)
        {
            if (na == null)
            {
                //figure out which area is this one
                foreach (ModuleView na1 in theNeuronArray.modules)
                {
                    if (na1.TheModule == this)
                    {
                        na = na1;
                        break;
                    }
                }
            }

            if (initialized && !forceInit) return;
            initialized = true;

            Initialize();

            if (dlg != null)
                Application.Current.Dispatcher.Invoke((Action)delegate { dlg.Draw(); });

            if (dlg == null && dlgIsOpen)
            {
                ShowDialog();
                dlgIsOpen = true;
            }
        }

        public void CloseDlg()
        {
            if (dlg != null)
                dlg.Close();
        }

        public virtual void ShowDialog()
        {
            ApartmentState aps = Thread.CurrentThread.GetApartmentState();
            if (aps != ApartmentState.STA) return;
            Type t = this.GetType();
            Type t1 = Type.GetType(t.ToString() + "Dlg");
            if (t1 == null) return;
            if (dlg != null) dlg.Close();
            dlg = (ModuleBaseDlg)Activator.CreateInstance(t1);
            if (dlg == null) return;
            dlg.ParentModule = (ModuleBase)this;
            dlg.Closed += Dlg_Closed;
            dlg.LocationChanged += Dlg_LocationChanged;
            dlg.SizeChanged += Dlg_SizeChanged;
            if (dlgPos != new Point(0, 0))
            {
                dlg.Top = dlgPos.Y;
                dlg.Left = dlgPos.X;
            }
            else
            {
                dlg.Top = 250;
                dlg.Left = 250;
            }
            if (dlgSize != new Point(0, 0))
            {
                dlg.Width = dlgSize.X;
                dlg.Height = dlgSize.Y;
            }
            else
            {
                dlg.Width = 350;
                dlg.Height = 300;
            }
            //this hack is here because a file might load and create dialogs prior to the mainwindow opening
            //so the same functionality is called from within FileLoad
            Window mainWindow = Application.Current.MainWindow;
            if (mainWindow.GetType() == typeof(MainWindow))
                dlg.Owner = Application.Current.MainWindow;

            dlg.Show();
            dlgIsOpen = true;
        }

        //this hack is here because a file can load and create dialogs prior to the mainwindow opening
        public void SetDlgOwner(Window MainWindow)
        {
            if (dlg != null)
                dlg.Owner = MainWindow;
        }

        private void Dlg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            dlgSize = new Point()
            { Y = dlg.Height, X = dlg.Width };
        }

        private void Dlg_LocationChanged(object sender, EventArgs e)
        {
            dlgPos = new Point()
            { Y = dlg.Top, X = dlg.Left };
        }

        private void Dlg_Closed(object sender, EventArgs e)
        {
            dlg = null;
            dlgIsOpen = false;
        }

        //this is called to allow for any data massaging needed before saving the file
        public virtual void SetUpBeforeSave()
        { }
        //this is called to allow for any data massaging needed after loading the file
        public virtual void SetUpAfterLoad()
        { }

        protected ModuleBase FindModuleByType(Type t)
        {
            foreach (ModuleView na1 in theNeuronArray.modules)
            {
                if (na1.TheModule != null && na1.TheModule.GetType() == t)
                {
                    return na1.TheModule;
                }
            }
            return null;
        }

        protected ModuleBase FindModuleByName(string name)
        {
            foreach (ModuleView na1 in theNeuronArray.modules)
            {
                if (na1.Label == name)
                {
                    return na1.TheModule;
                }
            }
            return null;
        }

        protected bool SetNeuronValue(string moduleName, string neuronLabel, float value)
        {
            bool retVal = false;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                Neuron n = naModule.GetNeuronAt(neuronLabel);
                if (n != null)
                {
                    n.SetValue(value);
                    retVal = true;
                }
            }
            return retVal;
        }

        protected float GetNeuronValue(string moduleName, string neuronLabel)
        {
            float retVal = 0;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                Neuron n = naModule.GetNeuronAt(neuronLabel);
                if (n != null)
                {
                    retVal = n.LastCharge;
                }
            }
            return retVal;
        }

        protected bool SetNeuronValue(string moduleName, int x, int y, float value)
        {
            bool retVal = false;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                Neuron n = naModule.GetNeuronAt(x, y);
                if (n != null)
                {
                    n.SetValue(value);
                    retVal = true;
                }
            }
            return retVal;
        }
        protected float GetNeuronValue(string moduleName, int x, int y)
        {
            float retVal = 0;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                Neuron n = naModule.GetNeuronAt(x, y);
                if (n != null)
                {
                    retVal = n.CurrentCharge;
                }
            }
            return retVal;
        }

        protected bool SetNeuronVector(string moduleName, bool isHoriz, int rowCol, float[] values)
        {
            bool retVal = true;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Neuron n;
                    if (isHoriz) n = naModule.GetNeuronAt(i, rowCol);
                    else n = naModule.GetNeuronAt(rowCol, i);
                    if (n != null)
                    {
                        n.SetValue(values[i]);
                    }
                    else retVal = false;
                }
            }
            else retVal = false;
            return retVal;
        }

        protected float[] GetNeuronVector(string moduleName, bool isHoriz, int rowCol)
        {
            float[] retVal;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                if (isHoriz)
                {
                    retVal = new float[naModule.Width];
                    for (int i = 0; i < retVal.Length; i++)
                        retVal[i] = naModule.GetNeuronAt(i, rowCol).CurrentCharge;
                }
                else
                {
                    retVal = new float[naModule.Height];
                    for (int i = 0; i < retVal.Length; i++)
                        retVal[i] = naModule.GetNeuronAt(rowCol, i).CurrentCharge;
                }
            }
            else
            {
                retVal = new float[0];
            }
            return retVal;
        }

        protected bool SetNeuronVector(string moduleName, bool isHoriz, int rowCol, int[] values)
        {
            bool retVal = true;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    Neuron n;
                    if (isHoriz) n = naModule.GetNeuronAt(i, rowCol);
                    else n = naModule.GetNeuronAt(rowCol, i);
                    if (n != null)
                    {
                        n.SetValueInt(values[i]);
                    }
                    else retVal = false;
                }
            }
            else retVal = false;
            return retVal;
        }

        protected int GetModuleWidth(string moduleName)
        {
            int retVal = 0;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null) retVal = naModule.Width;
            return retVal;
        }

        protected int GetModuleHeight(string moduleName)
        {
            int retVal = 0;
            ModuleView naModule;
            if (moduleName != null)
                naModule = theNeuronArray.FindAreaByLabel(moduleName);
            else
                naModule = na;
            if (naModule != null) retVal = naModule.Height;
            return retVal;
        }
    }
}
