//-----------------------------------------------------------------------
// <copyright file="Helper.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Geometry;

    /// <summary>
    /// Helper class
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// convert IUnit in esriUnits
        /// </summary>
        /// <param name="unit">object Unit</param>
        /// <returns>value of enumerator esriUnits</returns>
        internal static esriUnits ConvertUnitType(IUnit unit)
        {
            switch (unit.FactoryCode)
            {
                case 109006:
                    return esriUnits.esriCentimeters;
                case 9102:
                    return esriUnits.esriDecimalDegrees;
                case 109005:
                    return esriUnits.esriDecimeters;
                case 9003:
                    return esriUnits.esriFeet;
                case 109008:
                    return esriUnits.esriInches;
                case 9036:
                    return esriUnits.esriKilometers;
                case 9001:
                    return esriUnits.esriMeters;
                case 9035:
                    return esriUnits.esriMiles;
                case 109007:
                    return esriUnits.esriMillimeters;
                case 9030:
                    return esriUnits.esriNauticalMiles;
                case 109002:
                    return esriUnits.esriYards;
                default:
                    return esriUnits.esriUnknownUnits;
            }
        }
    }
}
