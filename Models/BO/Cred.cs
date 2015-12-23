﻿// This file contains my intellectual property. Release of this file requires prior approval from me.
// 
// Copyright (c) 2015, v0v All Rights Reserved

using System;
using System.Threading.Tasks;
using Interfaces.Enums;
using Interfaces.Models;
using Models.Factories;

namespace Models.BO
{
    public class Cred : ICred
    {
        #region ICred Members

        public short Autorization { get; set; }
        public string Cookie { get; set; }
        public DateTime Expired { get; set; }
        public string Login { get; set; }
        public string Pass { get; set; }
        public SiteType Site { get; set; }
        public string SiteAdress { get; set; }

        public async Task DeleteCredAsync()
        {
            await CredFactory.DeleteCredAsync(SiteAdress);
        }

        public async Task InsertCredAsync()
        {
            // await ((CredFactory) ServiceLocator.CredFactory).InsertCRedAsync(this);
            await CredFactory.InsertCredAsync(this);
        }

        public async Task UpdateAutorizationAsync(short autorize)
        {
            await CredFactory.UpdateAutorizationAsync(SiteAdress, autorize);
        }

        public async Task UpdateLoginAsync(string newlogin)
        {
            await CredFactory.UpdateLoginAsync(SiteAdress, newlogin);
        }

        public async Task UpdatePasswordAsync(string newpassword)
        {
            await CredFactory.UpdatePasswordAsync(SiteAdress, newpassword);
        }

        #endregion
    }
}
