﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System.Windows;
using System.Windows.Input;

namespace Crawler.Views
{
    /// <summary>
    ///     Interaction logic for EditDescriptionView.xaml
    /// </summary>
    public partial class EditDescriptionView : Window
    {
        #region Constructors

        public EditDescriptionView()
        {
            InitializeComponent();
            KeyDown += ViewKeyDown;
        }

        #endregion

        #region Event Handling

        private void ViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyDown -= ViewKeyDown;
                Close();
            }
        }

        #endregion
    }
}
