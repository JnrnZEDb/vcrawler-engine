﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// Copyright (c) 2015, v0v All Rights Reserved

using Interfaces.POCO;

namespace DataAPI.POCO
{
    public class SubtitlePOCO : ISubtitlePOCO
    {
        #region ISubtitlePOCO Members

        public string Language { get; set; }

        #endregion
    }
}