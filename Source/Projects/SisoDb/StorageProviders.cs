﻿using System;

namespace SisoDb
{
    [Serializable]
    public enum StorageProviders
    {
        Sql2005 = 100,
        Sql2008 = 200,
		Sql2012 = 300,
        SqlAzure = 350,
        SqlCe4 = 400,
    }
}