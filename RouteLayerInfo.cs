//-----------------------------------------------------------------------
// <copyright file="RouteLayerInfo.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System.Text;
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.SOESupport;

    /// <summary>
    /// class of RouteLayerInfo
    /// </summary>
    public class RouteLayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteLayerInfo"/> class
        /// </summary>
        /// <param name="mapLayerInfo">object IMapLayerInfo</param>
        public RouteLayerInfo(IMapLayerInfo mapLayerInfo)
        {
            this.Name = mapLayerInfo.Name;
            this.Id = mapLayerInfo.ID;
            this.Extent = mapLayerInfo.Extent;
        }

        /// <summary>
        /// Gets or sets extent
        /// </summary>
        public IEnvelope Extent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets id
        /// </summary>
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// convert the current instance in JsonObject
        /// </summary>
        /// <returns>object JsonObject</returns>
        public JsonObject ToJsonObject()
        {
            byte[] bytes = Conversion.ToJson(this.Extent);
            JsonObject jo = new JsonObject(Encoding.UTF8.GetString(bytes));
            IEnvelope envelope = this.Extent;
            if (!jo.Exists("spatialReference"))
            {
                if (envelope.SpatialReference.FactoryCode == 0)
                {
                    jo.AddString("spatialReference", envelope.SpatialReference.Name);
                }
                else
                {
                    jo.AddLong("spatialReference", (long)envelope.SpatialReference.FactoryCode);
                }
            }

            JsonObject jsonObjectRouteLayerInfo = new JsonObject();
            jsonObjectRouteLayerInfo.AddString("name", this.Name);
            jsonObjectRouteLayerInfo.AddLong("id", (long)this.Id);
            jsonObjectRouteLayerInfo.AddJsonObject("extent", jo);
            return jsonObjectRouteLayerInfo;
        }
    }
}
