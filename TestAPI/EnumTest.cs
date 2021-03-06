﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// 
// Copyright (c) 2015, v0v All Rights Reserved

using Extensions.Helpers;
using Interfaces.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestAPI
{
    [TestClass]
    public class EnumTest
    {
        #region Methods

        [TestMethod]
        public void TestPrivacyStatus()
        {
            Assert.AreEqual(PrivacyStatus.Private, EnumHelper.GetValueFromDescription<PrivacyStatus>("private"));
            Assert.AreEqual(PrivacyStatus.Public, EnumHelper.GetValueFromDescription<PrivacyStatus>("public"));
            Assert.AreEqual(PrivacyStatus.Unlisted, EnumHelper.GetValueFromDescription<PrivacyStatus>("unlisted"));

            Assert.AreEqual("private", EnumHelper.GetAttributeOfType(PrivacyStatus.Private));
            Assert.AreEqual("public", EnumHelper.GetAttributeOfType(PrivacyStatus.Public));
            Assert.AreEqual("unlisted", EnumHelper.GetAttributeOfType(PrivacyStatus.Unlisted));
        }

        #endregion
    }
}
