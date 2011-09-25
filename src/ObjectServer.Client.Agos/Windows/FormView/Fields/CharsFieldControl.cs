﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Malt.Layout.Widgets;

using ObjectServer.Client.Agos.Models;

namespace ObjectServer.Client.Agos.Windows.FormView
{
    public class CharsFieldControl : TextBox, IFieldWidget
    {
        private readonly IDictionary<string, object> metaField;

        public CharsFieldControl(object metaField)
        {
            this.metaField = (IDictionary<string, object>)metaField;
            this.FieldName = (string)this.metaField["name"];

            this.DefaultStyleKey = typeof(TextBox);

            this.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            this.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            this.Margin = new Thickness(5, 2, 5, 2);

            if (!this.IsReadOnly && (bool)this.metaField["required"])
            {
                this.Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xcc));
            }

        }

        public string FieldName { get; private set; }

        public object Value
        {
            get
            {
                return this.Text;
            }
            set
            {
                this.Text = value as string ?? string.Empty;
            }
        }

        public void Empty()
        {
            this.Text = String.Empty;
        }
    }
}
