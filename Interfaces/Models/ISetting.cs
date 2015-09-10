﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System.Threading.Tasks;

namespace Interfaces.Models
{
    public interface ISetting
    {
        #region Properties

        string Key { get; set; }
        string Value { get; set; }

        #endregion

        #region Methods

        Task DeleteSettingAsync();

        Task InsertSettingAsync();

        Task UpdateSettingAsync(string newvalue);

        #endregion
    }
}
