//-----------------------------------------------------------------------
// <copyright file="DynamicSegmentationException.cs" company="Studio A&T s.r.l.">
//  Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// class Dynamic Segmentation Exception
    /// </summary>
    [Serializable]
    public class DynamicSegmentationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the DynamicSegmentationException class
        /// </summary>
        public DynamicSegmentationException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicSegmentationException class
        /// </summary>
        /// <param name="message">message error</param>
        public DynamicSegmentationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicSegmentationException class
        /// </summary>
        /// <param name="message">message error</param>
        /// <param name="innerException">object Exception</param>
        public DynamicSegmentationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DynamicSegmentationException class
        /// </summary>
        /// <param name="info">object SerializationInfo</param>
        /// <param name="context">object StreamingContext</param>
        protected DynamicSegmentationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
