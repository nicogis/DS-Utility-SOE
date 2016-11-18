//-----------------------------------------------------------------------
// <copyright file="DSUtility.cs" company="Studio A&T s.r.l.">
//     Copyright (c) Studio A&T s.r.l. All rights reserved.
// </copyright>
// <author>Nicogis</author>
//-----------------------------------------------------------------------
namespace Studioat.ArcGis.Soe.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using ESRI.ArcGIS;
    using ESRI.ArcGIS.Carto;
    using ESRI.ArcGIS.esriSystem;
    using ESRI.ArcGIS.Geodatabase;
    using ESRI.ArcGIS.Geometry;
    using ESRI.ArcGIS.Location;
    using ESRI.ArcGIS.Server;
    using ESRI.ArcGIS.SOESupport;

    /// <summary>
    /// class Dynamic Segmentation Utility
    /// </summary>
    [ComVisible(true)]
    [Guid("dc829495-de6f-4c81-acb0-3e5ef225f654")]    
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
        AllCapabilities = "Point Location,Line Location,Identify Route,Identify Route Ex",
        DefaultCapabilities = "Point Location,Line Location,Identify Route",
        Description = "Dynamic Segmentation Utility",
        DisplayName = "Dynamic Segmentation Utility",
        Properties = "",
        HasManagerPropertiesConfigurationPane = false,
        SupportsREST = true,
        SupportsSOAP = false)]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Warning StyleCop - Code ESRI - pSOH")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Warning FxCop - Code ESRI - Capabilities")]
    public class DSUtility : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        /// <summary>
        /// name of soe
        /// </summary>
        private string soeName;

        /// <summary>
        /// Helper ServerObject
        /// </summary>
        private IServerObjectHelper serverObjectHelper;

        /// <summary>
        /// request handler
        /// </summary>
        private IRESTRequestHandler reqHandler;

        /// <summary>
        /// List of RouteLayerInfo
        /// </summary>
        private List<RouteLayerInfo> routeLayerInfos;

        /// <summary>
        /// version arcgis server
        /// </summary>
        private string agsVersion = "Unknown";

        /// <summary>
        /// Initializes a new instance of the <see cref="DSUtility"/> class
        /// </summary>
        public DSUtility()
        {
            this.soeName = this.GetType().Name;
            this.reqHandler = new SoeRestImpl(this.soeName, this.CreateRestSchema()) as IRESTRequestHandler;
        }

        /// <summary>
        /// input name operation of measure unit
        /// </summary>
        private enum MeasureUnit
        {
            /// <summary>
            /// route Measure Unit
            /// </summary>
            routeMeasureUnit,

            /// <summary>
            /// route Location Measure Unit
            /// </summary>
            routeLocationMeasureUnit
        }

        #region IServerObjectExtension Members
        /// <summary>
        /// init event of soe
        /// </summary>
        /// <param name="pSOH">Helper ServerObject</param>
        public void Init(IServerObjectHelper pSOH)
        {
            this.serverObjectHelper = pSOH;
            this.agsVersion = RuntimeManager.ActiveRuntime.Version;
        }

        /// <summary>
        /// shutdown event of soe
        /// </summary>
        public void Shutdown()
        {
        }

        #endregion

        #region IObjectConstruct Members

        /// <summary>
        /// costruct event of soe
        /// </summary>
        /// <param name="props">properties of soe</param>
        public void Construct(IPropertySet props)
        {
            this.routeLayerInfos = new List<RouteLayerInfo>();
            this.GetRouteLayerInfos();
        }

        #endregion

        #region IRESTRequestHandler Members

        /// <summary>
        /// get schema of soe
        /// </summary>
        /// <returns>schema of soe</returns>
        public string GetSchema()
        {
            return this.reqHandler.GetSchema();
        }

        /// <summary>
        /// Handler of request rest
        /// </summary>
        /// <param name="capabilities">capabilities of soe</param>
        /// <param name="resourceName">name of resource</param>
        /// <param name="operationName">name of operation</param>
        /// <param name="operationInput">input of operation</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>response in byte</returns>
        public byte[] HandleRESTRequest(string capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return this.reqHandler.HandleRESTRequest(capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        /// <summary>
        /// Get from input of operation the routeIDFieldName
        /// </summary>
        /// <param name="operationInput">input of operation</param>
        /// <param name="routeIDFieldNameDefault">value of default if routeIDFieldName is missing</param>
        /// <returns>value of RouteIDFieldName</returns>
        private static string GetRouteIDFieldName(JsonObject operationInput, string routeIDFieldNameDefault)
        {
            string routeIDFieldNameValue;
            bool found = operationInput.TryGetString("routeIDFieldName", out routeIDFieldNameValue);
            if (!found || string.IsNullOrEmpty(routeIDFieldNameValue))
            {
                if (routeIDFieldNameDefault == null)
                {
                    throw new DynamicSegmentationException("routeIDFieldName");
                }
                else
                {
                    routeIDFieldNameValue = routeIDFieldNameDefault;
                }
            }

            return routeIDFieldNameValue;
        }

        /// <summary>
        /// Get from input of operation the routeIDFieldName
        /// </summary>
        /// <param name="operationInput">input of operation</param>
        /// <returns>value of RouteIDFieldName</returns>
        private static string GetRouteIDFieldName(JsonObject operationInput)
        {
            return DSUtility.GetRouteIDFieldName(operationInput, null);
        }

        /// <summary>
        /// Get from input of operation the routeMeasureUnit
        /// </summary>
        /// <param name="operationInput">input of operation</param>
        /// <param name="inputName">name of operation input measure unit</param>
        /// <returns>value of routeMeasureUnit</returns>
        private static esriUnits GetMeasureUnit(JsonObject operationInput, MeasureUnit inputName)
        {
            string routeMeasureUnitValue;
            esriUnits routeMeasureUnit = esriUnits.esriUnknownUnits;

            bool found = operationInput.TryGetString(Enum.GetName(typeof(MeasureUnit), inputName), out routeMeasureUnitValue);
            if (found && !string.IsNullOrEmpty(routeMeasureUnitValue))
            {
                if (Enum.IsDefined(typeof(esriUnits), routeMeasureUnitValue))
                {
                    routeMeasureUnit = (esriUnits)Enum.Parse(typeof(esriUnits), routeMeasureUnitValue, true);
                }
            }

            return routeMeasureUnit;
        }

        /// <summary>
        /// Get from input of operation the SegmentExtension
        /// </summary>
        /// <param name="operationInput">input of operation</param>
        /// <returns>value of SegmentExtension</returns>
        private static esriSegmentExtension GetSegmentExtension(JsonObject operationInput)
        {
            string segmentExtensionValue;
            esriSegmentExtension segmentExtension = esriSegmentExtension.esriNoExtension;
            bool found = operationInput.TryGetString("segmentExtension", out segmentExtensionValue);
            if (found && !string.IsNullOrEmpty(segmentExtensionValue))
            {
                if (Enum.IsDefined(typeof(esriSegmentExtension), segmentExtensionValue))
                {
                    segmentExtension = (esriSegmentExtension)Enum.Parse(typeof(esriSegmentExtension), segmentExtensionValue, true);
                }
            }

            return segmentExtension;
        }

        /// <summary>
        /// get RouteLocator
        /// </summary>
        /// <param name="featureClass">object feature class</param>
        /// <param name="routeIDFieldNameValue">value of routeIDFieldName</param>
        /// <param name="routeMeasureUnit">unit of route measure</param>
        /// <returns>object IRouteLocator2</returns>
        private static IRouteLocator2 GetRouteLocator(IFeatureClass featureClass, string routeIDFieldNameValue, esriUnits routeMeasureUnit)
        {
            IDataset dataset = featureClass as IDataset;
            IName nameDataset = dataset.FullName;
            IRouteLocatorName routeMeasureLocatorName = new RouteMeasureLocatorNameClass();
            routeMeasureLocatorName.RouteFeatureClassName = nameDataset;
            routeMeasureLocatorName.RouteIDFieldName = routeIDFieldNameValue;
            routeMeasureLocatorName.RouteMeasureUnit = routeMeasureUnit;
            routeMeasureLocatorName.RouteWhereClause = string.Empty;
            nameDataset = (IName)routeMeasureLocatorName;
            return nameDataset.Open() as IRouteLocator2;
        }

        /// <summary>
        /// get RouteLayerInfo from Id
        /// </summary>
        /// <param name="routeLayerID">value of routeLayerID</param>
        /// <returns>object RouteLayerInfo</returns>
        private RouteLayerInfo GetRouteLayerInfo(int routeLayerID)
        {
            if (routeLayerID < 0)
            {
                throw new ArgumentOutOfRangeException("routeLayerID");
            }

            IMapServer3 serverObject = this.GetMapServer();
            IMapLayerInfos mapLayerInfos = serverObject.GetServerInfo(serverObject.DefaultMapName).MapLayerInfos;
            long count = mapLayerInfos.Count;
            for (int i = 0; i < count; i++)
            {
                IMapLayerInfo mapLayerInfo = mapLayerInfos.get_Element(i);
                if (mapLayerInfo.ID == routeLayerID)
                {
                    return new RouteLayerInfo(mapLayerInfo);
                }
            }

            throw new ArgumentOutOfRangeException("routeLayerID");
        }

        /// <summary>
        /// Feature Class from id of layer
        /// </summary>
        /// <param name="routelayerID">id route layer</param>
        /// <returns>feature class</returns>
        private IFeatureClass GetRouteFeatureClass(int routelayerID)
        {
            IMapServer3 mapServer = this.GetMapServer();
            IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
            return (IFeatureClass)dataAccess.GetDataSource(mapServer.DefaultMapName, routelayerID);
        }

        /// <summary>
        /// From service fills list of layer with M
        /// </summary>
        private void GetRouteLayerInfos()
        {
            IMapServer3 serverObject = this.GetMapServer();
            IMapLayerInfos mapLayerInfos = serverObject.GetServerInfo(serverObject.DefaultMapName).MapLayerInfos;

            this.routeLayerInfos = new List<RouteLayerInfo>();
            for (int i = 0; i < mapLayerInfos.Count; i++)
            {
                IMapLayerInfo mapLayerInfo = mapLayerInfos.get_Element(i);
                if (mapLayerInfo.IsFeatureLayer)
                {
                    IFields fields = mapLayerInfo.Fields;
                    for (int j = 0; j < fields.FieldCount; j++)
                    {
                        IField field = fields.get_Field(j);
                        if (field.Type == esriFieldType.esriFieldTypeGeometry)
                        {
                            IGeometryDef geometryDef = field.GeometryDef;
                            if (geometryDef.HasM)
                            {
                                this.routeLayerInfos.Add(new RouteLayerInfo(mapLayerInfo));
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get object MapServer of ServerObject 
        /// </summary>
        /// <returns>object MapServer</returns>
        private IMapServer3 GetMapServer()
        {
            IMapServer3 mapServer = this.serverObjectHelper.ServerObject as IMapServer3;
            if (mapServer == null)
            {
                throw new DynamicSegmentationException("Unable to access the map server.");
            }

            return mapServer;
        }

        /// <summary>
        /// create schema of soe
        /// </summary>
        /// <returns>resource of soe</returns>
        private RestResource CreateRestSchema()
        {
            RestResource rootRes = new RestResource(this.soeName, false, this.RootResHandler);

            RestResource infoResource = new RestResource("Info", false, this.InfoResHandler);
            rootRes.resources.Add(infoResource);

            RestResource helpResource = new RestResource("Help", false, this.HelpResHandler);
            rootRes.resources.Add(helpResource);

            RestResource item = new RestResource("RouteLayers", true, new ResourceHandler(this.RouteLayer));

            RestOperation pointLocation = new RestOperation("PointLocation", new string[] { "routeIDFieldName", "routeID", "measure", "lateralOffset", "routeMeasureUnit", "routeLocationMeasureUnit" }, new string[] { "json" }, this.PointLocationOperHandler, "Point Location");
            RestOperation lineLocation = new RestOperation("LineLocation", new string[] { "routeIDFieldName", "routeID", "fromMeasure", "toMeasure", "lateralOffset", "routeMeasureUnit", "routeLocationMeasureUnit" }, new string[] { "json" }, this.LineLocationOperHandler, "Line Location");
            RestOperation identifyRoute = new RestOperation("IdentifyRoute", new string[] { "location", "tolerance", "routeMeasureUnit", "routeIDFieldName" }, new string[] { "json" }, this.IdentifyRouteOperHandler, "Identify Route");
            RestOperation identifyRouteEx = new RestOperation("IdentifyRouteEx", new string[] { "location", "tolerance", "routeID", "routeMeasureUnit", "routeIDFieldName", "segmentExtension" }, new string[] { "json" }, this.IdentifyRouteExOperHandler, "Identify Route Ex");
            
            item.operations.Add(pointLocation);
            item.operations.Add(lineLocation);
            item.operations.Add(identifyRoute);
            item.operations.Add(identifyRouteEx);
            rootRes.resources.Add(item);
            return rootRes;
        }

        /// <summary>
        /// handler of resource root
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>root resource in format output in byte</returns>
        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            JsonObject[] objectArray = System.Array.ConvertAll(this.routeLayerInfos.ToArray(), i => i.ToJsonObject());

            JsonObject jsonObject = new JsonObject();
            jsonObject.AddString("Description", "Dynamic Segmentation Utility SOE Rest");
            jsonObject.AddArray("routeLayers", objectArray);
            return jsonObject.JsonByte();
        }

        /// <summary>
        /// Returns JSON representation of Info resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>String JSON representation of Info resource.</returns>
        private byte[] InfoResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();
            AddInPackageAttribute addInPackage = (AddInPackageAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AddInPackageAttribute), false)[0];
            result.AddString("agsVersion", addInPackage.TargetVersion);
            result.AddString("soeVersion", addInPackage.Version);
            result.AddString("author", addInPackage.Author);
            result.AddString("company", addInPackage.Company);

            return result.JsonByte();
        }

        /// <summary>
        /// Returns JSON representation of Help resource. This resource is not a collection.
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>String JSON representation of Help resource.</returns>
        private byte[] HelpResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";

            JsonObject result = new JsonObject();

            JsonObject soeResources = new JsonObject();
            soeResources.AddString("RouteLayers", "A list of polylineM layers in the map.");
            result.AddJsonObject("Resources", soeResources);

            JsonObject getIdentifyRouteInputs = new JsonObject();
            getIdentifyRouteInputs.AddString("location", "(geometry) Point");
            getIdentifyRouteInputs.AddString("tolerance", "(number) optional but if you don't set, soe set 0 so you could no have results");
            getIdentifyRouteInputs.AddString("routeMeasureUnit", "(enum) optional default is esriUnknownUnits");
            getIdentifyRouteInputs.AddString("routeIDFieldName", "(string)");

            JsonObject getIdentifyRouteOutput = new JsonObject();
            JsonObject getIdentifyRouteOutputLoc = new JsonObject();
            getIdentifyRouteOutputLoc.AddString("routeID", "(string, double, int) depends fieldtype routeIDFieldName");
            getIdentifyRouteOutputLoc.AddString("measure", "(number)");
            getIdentifyRouteOutput.AddArray("location (array)", new JsonObject[] { getIdentifyRouteOutputLoc });

            JsonObject getIdentifyRouteParams = new JsonObject();
            getIdentifyRouteParams.AddString("Info", "Identify Route operation. To learn more about formatting the input geometries, input geometry, please visit the 'Geometry Objects' section of the ArcGIS Server REST documentation.");
            getIdentifyRouteParams.AddJsonObject("Inputs", getIdentifyRouteInputs);
            getIdentifyRouteParams.AddJsonObject("Outputs", getIdentifyRouteOutput);
            
            JsonObject getPointLocationInputs = new JsonObject();
            getPointLocationInputs.AddString("routeIDFieldName", "(string)");
            getPointLocationInputs.AddString("routeID", "(string, double, int) depends fieldtype routeIDFieldName");
            getPointLocationInputs.AddString("measure", "(number)");
            getPointLocationInputs.AddString("lateralOffset", "(number) optional default = 0");
            getPointLocationInputs.AddString("routeMeasureUnit", "(enum) optional default = esriUnknownUnits");
            getPointLocationInputs.AddString("routeLocationMeasureUnit", "(enum) optional default = esriUnknownUnits");
            
            JsonObject getPointLocationOutput = new JsonObject();
            getPointLocationOutput.AddString("geometries", "(geometry) point or multipoint");

            JsonObject getPointLocationParams = new JsonObject();
            getPointLocationParams.AddString("Info", "Point Location operation.");
            getPointLocationParams.AddJsonObject("Inputs", getPointLocationInputs);
            getPointLocationParams.AddJsonObject("Outputs", getPointLocationOutput);

            JsonObject getLineLocationInputs = new JsonObject();
            getLineLocationInputs.AddString("routeIDFieldName", "(string)");
            getLineLocationInputs.AddString("routeID", "(string, double, int) depends fieldtype routeIDFieldName");
            getLineLocationInputs.AddString("fromMeasure", "(number) optional if you set toMeasure");
            getLineLocationInputs.AddString("toMeasure", "(number) optional if you set fromMeasure");
            getLineLocationInputs.AddString("lateralOffset", "(number) optional default = 0");
            getLineLocationInputs.AddString("routeMeasureUnit", "(enum) optional default = esriUnknownUnits");
            getLineLocationInputs.AddString("routeLocationMeasureUnit", "(enum) optional default = esriUnknownUnits");

            JsonObject getLineLocationOutput = new JsonObject();
            getLineLocationOutput.AddString("geometries", "(geometry) polyline");

            JsonObject getLineLocationParams = new JsonObject();
            getLineLocationParams.AddString("Info", "Line Location operation.");
            getLineLocationParams.AddJsonObject("Inputs", getLineLocationInputs);
            getLineLocationParams.AddJsonObject("Outputs", getLineLocationOutput);

            JsonObject getIdentifyRouteExInputs = new JsonObject();
            getIdentifyRouteExInputs.AddString("location", "(geometry) Point");
            getIdentifyRouteExInputs.AddString("routeID", "(string, double, int) depends fieldtype routeIDFieldName");
            getIdentifyRouteExInputs.AddString("tolerance", "(number) optional but if you don't set, soe set 0 so you could no have results");
            getIdentifyRouteExInputs.AddString("routeMeasureUnit", "(enum) optional default = esriUnknownUnits");
            getIdentifyRouteExInputs.AddString("routeIDFieldName", "(string)");
            getIdentifyRouteExInputs.AddString("segmentExtension", "(enum) optional default = esriNoExtension");

            JsonObject getIdentifyRouteExOutput = new JsonObject();
            JsonObject getIdentifyRouteExOutputLoc = new JsonObject();
            getIdentifyRouteExOutputLoc.AddString("routeID", "(string, double, int) depends fieldtype routeIDFieldName");
            getIdentifyRouteExOutputLoc.AddString("measure", "(number)");
            getIdentifyRouteExOutputLoc.AddString("location", "(geometry) point");

            getIdentifyRouteExOutput.AddArray("location (array)", new JsonObject[] { getIdentifyRouteExOutputLoc });

            JsonObject getIdentifyRouteExParams = new JsonObject();
            getIdentifyRouteExParams.AddString("Info", "Identify Route Ex operation. To learn more about formatting the input geometries, input geometry, please visit the 'Geometry Objects' section of the ArcGIS Server REST documentation.");
            getIdentifyRouteExParams.AddJsonObject("Inputs", getIdentifyRouteExInputs);
            getIdentifyRouteExParams.AddJsonObject("Outputs", getIdentifyRouteExOutput);
            
            JsonObject soeOperations = new JsonObject();
            soeOperations.AddJsonObject("IdentifyRoute", getIdentifyRouteParams);
            soeOperations.AddJsonObject("PointLocation", getPointLocationParams);
            soeOperations.AddJsonObject("LineLocation", getLineLocationParams);
            soeOperations.AddJsonObject("IdentifyRouteEx", getIdentifyRouteExParams);

            result.AddJsonObject("Operations", soeOperations);

            return result.JsonByte();
        }

        /// <summary>
        /// resource RouteLayer
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>resource in byte</returns>
        private byte[] RouteLayer(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            if (boundVariables["RouteLayersID"] == null)
            {
                JsonObject[] objectArray = System.Array.ConvertAll(this.routeLayerInfos.ToArray(), i => i.ToJsonObject());
                JsonObject jsonObject = new JsonObject();
                jsonObject.AddArray("routeLayers", objectArray);
                return jsonObject.JsonByte();
            }
            else
            {
                int layerID = Convert.ToInt32(boundVariables["RouteLayersID"], CultureInfo.InvariantCulture);
                return this.GetRouteLayerInfo(layerID).ToJsonObject().JsonByte();
            }
        }

        /// <summary>
        /// Handler operation Point Location
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="operationInput">input of operation</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>response in byte</returns>
        private byte[] PointLocationOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            
            int routeLayerID = Convert.ToInt32(boundVariables["RouteLayersID"], CultureInfo.InvariantCulture);

            esriUnits routeMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeMeasureUnit);
            esriUnits routeLocationMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeLocationMeasureUnit);

            double? measure;
            bool found = operationInput.TryGetAsDouble("measure", out measure);
            if (!found || !measure.HasValue)
            {
                throw new DynamicSegmentationException("measure not valid");
            }

            double? lateralOffset;
            found = operationInput.TryGetAsDouble("lateralOffset", out lateralOffset);
            if (!found || !lateralOffset.HasValue)
            {
                lateralOffset = 0;
            }

            IFeatureClass featureClass = this.GetRouteFeatureClass(routeLayerID);

            string routeIDFieldNameValue = DSUtility.GetRouteIDFieldName(operationInput);
            
            IFields fields = featureClass.Fields;
            int indexField = fields.FindField(routeIDFieldNameValue);
            if (indexField == -1)
            {
                throw new DynamicSegmentationException(string.Format(CultureInfo.InvariantCulture, "routeIDFieldName {0} not found!", routeIDFieldNameValue));
            }

            object routeID;
            found = operationInput.TryGetObject("routeID", out routeID);
            if (!found)
            {
                throw new DynamicSegmentationException("routeID not valid");
            }

            IField field = fields.get_Field(indexField);
            this.CheckRouteID(routeID, field);

            IRouteLocator2 routeLocator = DSUtility.GetRouteLocator(featureClass, routeIDFieldNameValue, routeMeasureUnit);

            IRouteLocation routeLocation = new RouteMeasurePointLocationClass();
            routeLocation.RouteID = routeID;
            routeLocation.MeasureUnit = routeLocationMeasureUnit;
            routeLocation.LateralOffset = lateralOffset.Value;

            IRouteMeasurePointLocation routeMeasurePointLocation = (IRouteMeasurePointLocation)routeLocation;
            routeMeasurePointLocation.Measure = measure.Value;

            IGeometry geometry;
            esriLocatingError locatingError;
            routeLocator.Locate(routeLocation, out geometry, out locatingError);
            int errorId = (int)locatingError;

            JsonObject result = null;

            if (errorId != 0)
            {
                string errorDescription = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", Enum.GetName(typeof(esriLocatingError), locatingError), errorId);
                IObjectJson jsonObjectError = new ObjectError(errorDescription);
                result = jsonObjectError.ToJsonObject();
            }

            if ((geometry != null) && (!geometry.IsEmpty))
            {
                if (result == null)
                {
                    result = new JsonObject();
                }

                result.AddJsonObject("geometries", Conversion.ToJsonObject(geometry));
            }
            
            return result.JsonByte();
        }

        /// <summary>
        /// Handler operation Line Location
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="operationInput">input of operation</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>response in byte</returns>
        private byte[] LineLocationOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            int routeLayerID = Convert.ToInt32(boundVariables["RouteLayersID"], CultureInfo.InvariantCulture);

            esriUnits routeMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeMeasureUnit);
            esriUnits routeLocationMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeLocationMeasureUnit);

            double? fromMeasure;
            operationInput.TryGetAsDouble("fromMeasure", out fromMeasure);
          
            double? toMeasure;
            operationInput.TryGetAsDouble("toMeasure", out toMeasure);
            
            if (!fromMeasure.HasValue && !toMeasure.HasValue)
            {
                throw new DynamicSegmentationException("Set at least fromMeasure or toMeasure!");
            }

            double? lateralOffset;
            bool found = operationInput.TryGetAsDouble("lateralOffset", out lateralOffset);
            if (!found || !lateralOffset.HasValue)
            {
                lateralOffset = 0;
            }

            IFeatureClass featureClass = this.GetRouteFeatureClass(routeLayerID);

            string routeIDFieldNameValue = DSUtility.GetRouteIDFieldName(operationInput);
            IFields fields = featureClass.Fields;
            int indexField = fields.FindField(routeIDFieldNameValue);
            if (indexField == -1)
            {
                throw new DynamicSegmentationException(string.Format(CultureInfo.InvariantCulture, "routeIDFieldName {0} not found!", routeIDFieldNameValue));
            }

            object routeID;
            found = operationInput.TryGetObject("routeID", out routeID);
            if (!found)
            {
                throw new DynamicSegmentationException("routeID not valid");
            }

            IField field = fields.get_Field(indexField);
            this.CheckRouteID(routeID, field);
            
            IRouteLocator2 routeLocator = DSUtility.GetRouteLocator(featureClass, routeIDFieldNameValue, routeMeasureUnit);
            
            IRouteLocation routeLocation = new RouteMeasureLineLocationClass();
            routeLocation.RouteID = routeID;
            routeLocation.MeasureUnit = routeLocationMeasureUnit;
            routeLocation.LateralOffset = lateralOffset.Value;

            IRouteMeasureLineLocation routeMeasureLineLocation = (IRouteMeasureLineLocation)routeLocation;
            if (fromMeasure.HasValue)
            {
                routeMeasureLineLocation.FromMeasure = fromMeasure.Value;
            }

            if (toMeasure.HasValue)
            {
                routeMeasureLineLocation.ToMeasure = toMeasure.Value;
            }
            
            IGeometry geometry;
            esriLocatingError locatingError;
            routeLocator.Locate(routeLocation, out geometry, out locatingError);
            int errorId = (int)locatingError;

            JsonObject result = null;

            if (errorId != 0)
            {
                string errorDescription = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", Enum.GetName(typeof(esriLocatingError), locatingError), errorId);
                IObjectJson jsonObjectError = new ObjectError(errorDescription);
                result = jsonObjectError.ToJsonObject();
            }

            if ((geometry != null) && (!geometry.IsEmpty))
            {
                if (result == null)
                {
                    result = new JsonObject();
                }

                result.AddJsonObject("geometries", Conversion.ToJsonObject(geometry));
            }

            return result.JsonByte();
        }
        
        /// <summary>
        /// Handler operation Identify Route
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="operationInput">input of operation</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>response in byte</returns>
        private byte[] IdentifyRouteOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
                responseProperties = "{\"Content-Type\" : \"application/json\"}";
                string methodName = MethodBase.GetCurrentMethod().Name;
                int routeLayerID = Convert.ToInt32(boundVariables["RouteLayersID"], CultureInfo.InvariantCulture);

                esriUnits routeMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeMeasureUnit);

                IFeatureClass featureClass = this.GetRouteFeatureClass(routeLayerID);
                string routeIDFieldNameValue = DSUtility.GetRouteIDFieldName(operationInput, featureClass.OIDFieldName);
                IRouteLocator2 routeLocator = DSUtility.GetRouteLocator(featureClass, routeIDFieldNameValue, routeMeasureUnit);

                double? tolerance;
                bool found = operationInput.TryGetAsDouble("tolerance", out tolerance);
                if (!found || !tolerance.HasValue)
                {
                    tolerance = 0.0;
                }

                JsonObject jsonLocation;
                if (!operationInput.TryGetJsonObject("location", out jsonLocation))
                {
                    throw new ArgumentException("Invalid location", methodName);
                }
              
                IPoint location = Conversion.ToGeometry(jsonLocation, esriGeometryType.esriGeometryPoint) as IPoint;
                if (location == null)
                {
                    throw new ArgumentException("Invalid location", methodName);
                }

                IEnvelope envelope = location.Envelope;
                envelope.Expand(tolerance.Value, tolerance.Value, false);

                IRouteMeasurePointLocation routeMeasurePointLocation = new RouteMeasurePointLocationClass();
                IRouteLocation routeLocation;           
                IFeature feature;           
                JsonObject result = new JsonObject();
                
                List<JsonObject> measures = new List<JsonObject>();
                IEnumRouteIdentifyResult enumResult = routeLocator.Identify(envelope, string.Empty);           
                for (int i = 1; i <= enumResult.Count; i++)            
                {
                    enumResult.Next(out routeLocation, out feature);
                    routeMeasurePointLocation = (IRouteMeasurePointLocation)routeLocation;
                    JsonObject measure = new JsonObject();
                    measure.AddString("routeID", routeLocation.RouteID.ToString());
                    measure.AddDouble("measure", routeMeasurePointLocation.Measure);
                    measures.Add(measure);       
                }

                result.AddArray("location", measures.ToArray());

                return result.JsonByte();
        }

        /// <summary>
        /// Handler operation Identify Route Ex
        /// </summary>
        /// <param name="boundVariables">list of variables bound</param>
        /// <param name="operationInput">input of operation</param>
        /// <param name="outputFormat">format of output</param>
        /// <param name="requestProperties">list of request properties</param>
        /// <param name="responseProperties">list of response properties </param>
        /// <returns>response in byte</returns>
        private byte[] IdentifyRouteExOperHandler(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = "{\"Content-Type\" : \"application/json\"}";
            string methodName = MethodBase.GetCurrentMethod().Name;
            int routeLayerID = Convert.ToInt32(boundVariables["RouteLayersID"], CultureInfo.InvariantCulture);

            esriUnits routeMeasureUnit = DSUtility.GetMeasureUnit(operationInput, MeasureUnit.routeMeasureUnit);
            esriSegmentExtension segmentExtension = DSUtility.GetSegmentExtension(operationInput);
            IFeatureClass featureClass = this.GetRouteFeatureClass(routeLayerID);

            JsonObject jsonLocation;
            if (!operationInput.TryGetJsonObject("location", out jsonLocation))
            {
                throw new ArgumentException("Invalid location", methodName);
            }

            IPoint location = Conversion.ToGeometry(jsonLocation, esriGeometryType.esriGeometryPoint) as IPoint;
            if (location == null)
            {
                throw new ArgumentException("Invalid location", methodName);
            }

            string routeIDFieldNameValue = DSUtility.GetRouteIDFieldName(operationInput);
            IFields fields = featureClass.Fields;
            int indexField = fields.FindField(routeIDFieldNameValue);
            if (indexField == -1)
            {
                throw new DynamicSegmentationException(string.Format(CultureInfo.InvariantCulture, "routeIDFieldName {0} not found!", routeIDFieldNameValue));
            }

            object routeID;
            bool found = operationInput.TryGetObject("routeID", out routeID);
            if (!found)
            {
                throw new DynamicSegmentationException("routeID not valid");
            }

            double? tolerance;
            found = operationInput.TryGetAsDouble("tolerance", out tolerance);
            if (!found || !tolerance.HasValue)
            {
                tolerance = 0.0;
            }

            IField field = fields.get_Field(indexField);
            this.CheckRouteID(routeID, field);

            IRouteLocator2 routeLocator = DSUtility.GetRouteLocator(featureClass, routeIDFieldNameValue, routeMeasureUnit);

            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.SubFields = routeLocator.RouteIDFieldName;
            queryFilter.AddField(routeLocator.RouteFeatureClass.ShapeFieldName);
            string where = string.Format("{0} = {1}{2}{1}", routeLocator.RouteIDFieldNameDelimited, routeLocator.RouteIDIsString ? "'" : string.Empty, routeID);
            queryFilter.WhereClause = where;

            IPoint locationNearest = null;
            IFeatureCursor featureCursor = null;
            try
            {
                featureCursor = routeLocator.RouteFeatureClass.Search(queryFilter, true);
                IFeature featureRoute = featureCursor.NextFeature();
                if (featureRoute == null)
                {
                    throw new DynamicSegmentationException(string.Format(CultureInfo.InvariantCulture, "Feature with value {0} not found!", routeID));
                }
                
                IProximityOperator proximityOperator = featureRoute.ShapeCopy as IProximityOperator;
                locationNearest = proximityOperator.ReturnNearestPoint(location, segmentExtension);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (featureCursor != null)
                {
                    Marshal.ReleaseComObject(featureCursor);
                }
            }

            IEnvelope envelope = locationNearest.Envelope;
            envelope.Expand(tolerance.Value, tolerance.Value, false);

            IRouteMeasurePointLocation routeMeasurePointLocation = new RouteMeasurePointLocationClass();
            IRouteLocation routeLocation;
            IFeature feature;
            JsonObject result = new JsonObject();

            List<JsonObject> measures = new List<JsonObject>();
            IEnumRouteIdentifyResult enumResult = routeLocator.Identify(envelope, where);
            for (int i = 1; i <= enumResult.Count; i++)
            {
                enumResult.Next(out routeLocation, out feature);
                routeMeasurePointLocation = (IRouteMeasurePointLocation)routeLocation;
                JsonObject measure = new JsonObject();
                measure.AddString("routeID", routeLocation.RouteID.ToString());
                measure.AddDouble("measure", routeMeasurePointLocation.Measure);
                measure.AddJsonObject("location", Conversion.ToJsonObject(locationNearest, true));
                measures.Add(measure);
            }

            result.AddArray("location", measures.ToArray());

            return result.JsonByte();
        }

        /// <summary>
        /// check if value of RouteID is string, double or string and check definition of its field  
        /// </summary>
        /// <param name="routeID">value routeID</param>
        /// <param name="field">field of routeID</param>
        private void CheckRouteID(object routeID, IField field)
        {
            if ((field.Type == esriFieldType.esriFieldTypeInteger) || (field.Type == esriFieldType.esriFieldTypeOID))
            {
                try
                {
                    int.Parse(routeID.ToString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new FormatException("routeID must be integer!");
                }
            }
            else if (field.Type == esriFieldType.esriFieldTypeDouble)
            {
                try
                {
                    double.Parse(routeID.ToString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new FormatException("routeID must be double!");
                }
            }
            else if (field.Type == esriFieldType.esriFieldTypeString)
            {
            }
            else
            {
                throw new DynamicSegmentationException(string.Format(CultureInfo.InvariantCulture, "Field Type routeIDFieldName must be string, double or integer!", field.Name));
            }
        }
    }
}
