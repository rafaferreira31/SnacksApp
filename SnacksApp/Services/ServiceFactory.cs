﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnacksApp.Services
{
    public static class ServiceFactory
    {
        public static FavoritosService CreateFavoritosService()
        {
            return new FavoritosService();
        }
    }


}
