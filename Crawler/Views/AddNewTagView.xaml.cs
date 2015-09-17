﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using Crawler.ViewModels;
using Interfaces.Factories;
using Interfaces.Models;

namespace Crawler.Views
{
    /// <summary>
    ///     Interaction logic for AddNewTagView.xaml
    /// </summary>
    public partial class AddNewTagView : Window
    {
        #region Constructors

        public AddNewTagView()
        {
            InitializeComponent();
            KeyDown += AddNewTagKeyDown;
        }

        #endregion

        #region Event Handling

        private void AddNewTagKeyDown(object sender, KeyEventArgs e)
        {
            KeyDown -= AddNewTagKeyDown;
            if (e.Key == Key.Escape)
            {
                Close();
            }
            if (e.Key == Key.Enter)
            {
                // нажмем кнопку программно
                var peer = new ButtonAutomationPeer(buttonOk);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                if (invokeProv != null)
                {
                    invokeProv.Invoke();
                }
            }
        }

        private void AddNewTagView_OnLoaded(object sender, RoutedEventArgs e)
        {
            textBoxTag.Focus();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var mv = DataContext as MainWindowViewModel;
            if (mv == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(mv.Model.NewTag))
            {
                return;
            }
            ITagFactory tf = mv.Model.BaseFactory.CreateTagFactory();
            ITag tag = tf.CreateTag();
            tag.Title = mv.Model.NewTag;
            if (mv.Model.Tags.Select(x => x.Title).Contains(tag.Title))
            {
                return;
            }
            mv.Model.Tags.Add(tag);
            await tag.InsertTagAsync();
            mv.Model.NewTag = string.Empty;
            Close();
        }

        #endregion
    }
}
