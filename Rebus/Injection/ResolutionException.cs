﻿using System;

namespace Rebus.Injection;

/// <summary>
/// Exceptions that is thrown when something goes wrong while working with the injectionist
/// </summary>
[Serializable]
public class ResolutionException : Exception
{
    /// <summary>
    /// Constructs the exception
    /// </summary>
    public ResolutionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructs the exception
    /// </summary>
    public ResolutionException(Exception innerException, string message) : base(message, innerException)
    {
    }

}