﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using Interfaces.Models;

namespace Models.BO
{
    public class Setting : ISetting
    {
        #region ISetting Members

        public string Key { get; set; }
        public string Value { get; set; }

        #endregion
    }
}
