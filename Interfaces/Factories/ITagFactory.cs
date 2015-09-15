﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// Copyright (c) 2015, v0v All Rights Reserved

using Interfaces.Models;
using Interfaces.POCO;

namespace Interfaces.Factories
{
    public interface ITagFactory
    {
        #region Methods

        ITag CreateTag();

        ITag CreateTag(ITagPOCO poco);

        #endregion
    }
}
