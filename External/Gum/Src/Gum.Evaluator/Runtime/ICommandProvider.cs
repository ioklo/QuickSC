﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    public interface ICommandProvider
    {
        Task ExecuteAsync(string cmdText);
    }
}
