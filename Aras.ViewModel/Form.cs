﻿/*  
  Aras.ViewModel provides a .NET library for building Aras Innovator Applications

  Copyright (C) 2016 Processwall Limited.

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU Affero General Public License as published
  by the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License
  along with this program.  If not, see http://opensource.org/licenses/AGPL-3.0.
 
  Company: Processwall Limited
  Address: The Winnowing House, Mill Lane, Askham Richard, York, YO23 3NW, United Kingdom
  Tel:     +44 113 815 3440
  Email:   support@processwall.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aras.ViewModel
{
    public class Form : Control
    {
        [ViewModel.Attributes.Command("Edit")]
        public EditCommand Edit { get; private set; }

        [ViewModel.Attributes.Command("Save")]
        public SaveCommand Save { get; private set; }

        [ViewModel.Attributes.Command("Undo")]
        public UndoCommand Undo { get; private set; }

        [ViewModel.Attributes.Property("Properties", Aras.ViewModel.Attributes.PropertyTypes.ControlList, true)]
        public Model.ObservableList<Property> Fields { get; private set; }

        private Model.Transaction Transaction;

        private List<String> _propertyNames;
        public IEnumerable<String> PropertyNames
        {
            get
            {
                return this._propertyNames;
            }
        }

        public void AddPropertyNames(String Names)
        {
            foreach (String name in Names.Split(','))
            {
                if (!this._propertyNames.Contains(name))
                {
                    this._propertyNames.Add(name);
                }
            }
        }

        public override object Binding
        {
            get
            {
                return base.Binding;
            }
            set
            {
                if (value == null)
                {
                    base.Binding = value;
                }
                else
                {
                    if (value is Model.Item)
                    {
                        base.Binding = value;
                    }
                    else
                    {
                        throw new Model.Exceptions.ArgumentException("Binding must be of type Aras.Model.Item");
                    }
                }
            }
        }

        protected override void AfterBindingChanged()
        {
            base.AfterBindingChanged();

            this.Fields.NotifyListChanged = false;
            this.Fields.Clear();

            if (this.Binding != null)
            {
                this.Transaction = ((Model.Item)this.Binding).Transaction;

                foreach (String propertyname in this.PropertyNames)
                {
                    if (((Model.Item)this.Binding).HasProperty(propertyname))
                    {
                        Model.Property modelproperty = ((Model.Item)this.Binding).Property(propertyname);
                        ViewModel.Property property = null;

                        switch (modelproperty.Type.GetType().Name)
                        {
                            case "Float":
                                property = new Properties.Float();
                                break;
                            case "List":
                                property = new Properties.List();
                                break;
                            case "String":
                                property = new Properties.String();
                                break;
                            default:
                                throw new NotImplementedException("Property Type not implemented: " + ((Model.Item)this.Binding).HasProperty(propertyname));
                        }

                        property.Binding = modelproperty;
                        this.Fields.Add(property);
                    }
                }
            }
            else
            {
                this.Transaction = null;
            }

            this.Fields.NotifyListChanged = true;

            this.RefreshCommands();
        }

        private void RefreshCommands()
        {
            this.Edit.Refesh();
            this.Save.Refesh();
            this.Undo.Refesh();
        }

        protected override void RefreshControl()
        {
            base.RefreshControl();

            if (this.Transaction == null && this.Binding != null)
            {
                ((Model.Item)this.Binding).Refresh();
            }
        }

        public Form()
            :base()
        {
            this.Transaction = null;
            this._propertyNames = new List<String>();
            this.Fields = new Model.ObservableList<Property>();
            this.Edit = new EditCommand(this);
            this.Save = new SaveCommand(this);
            this.Undo = new UndoCommand(this);
        }

        public class EditCommand : Aras.ViewModel.Command
        {
            public Form Form { get; private set; }

            protected override bool Run(IEnumerable<Control> Parameters)
            {
                if (this.CanExecute)
                {
                    this.Form.Transaction = ((Model.Item)this.Form.Binding).Session.BeginTransaction();
                    ((Model.Item)this.Form.Binding).Update(this.Form.Transaction);
                }

                this.Form.RefreshCommands();

                return true;
            }

            internal void Refesh()
            {
                if ((this.Form.Transaction == null) && (this.Form.Binding != null) && ((Model.Item)this.Form.Binding).CanUpdate)
                {
                    this.CanExecute = true;
                }
                else
                {
                    this.CanExecute = false;
                }
            }

            internal EditCommand(Form Form)
            {
                this.Form = Form;
                this.Refesh();
            }
        }

        public class SaveCommand : Aras.ViewModel.Command
        {
            public Form Form { get; private set; }

            protected override bool Run(IEnumerable<Control> Parameters)
            {
                if (this.CanExecute)
                {
                    this.Form.Transaction.Commit();
                }

                this.Form.RefreshCommands();

                return true;
            }

            internal void Refesh()
            {
                if (this.Form.Transaction != null)
                {
                    this.CanExecute = true;
                }
                else
                {
                    this.CanExecute = false;
                }
            }

            internal SaveCommand(Form Form)
            {
                this.Form = Form;
                this.Refesh();
            }
        }

        public class UndoCommand : Aras.ViewModel.Command
        {
            public Form Form { get; private set; }

            protected override bool Run(IEnumerable<Control> Parameters)
            {
                if (this.CanExecute)
                {
                    this.Form.Transaction.RollBack();
                }

                this.Form.RefreshCommands();

                return true;
            }

            internal void Refesh()
            {
                if (this.Form.Transaction != null)
                {
                    this.CanExecute = true;
                }
                else
                {
                    this.CanExecute = false;
                }
            }

            internal UndoCommand(Form Form)
            {
                this.Form = Form;
                this.Refesh();
            }
        }

    }
}
