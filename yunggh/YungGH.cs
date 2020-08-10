using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Threading;
using System.Diagnostics;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino;
using Rhino.Geometry;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using System.Collections.Specialized;

namespace yunggh
{
    public class YungGH
    {
        public Box FitBoundingBox(Brep B, out Plane plane, out Vector3d normal, out Vector3d forward)
        {
            //get surface normal from largest surface of brep
            Point3d origin = new Point3d(0, 0, 0);
            normal = new Vector3d(0, 0, 1);
            double largestArea = 0;
            bool NoPlanarSurfacesFound = true;
            foreach (BrepFace brep in B.Faces)
            {
                if (!brep.IsPlanar()) continue;// we only want to use planar surfaces

                Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(brep);
                if (area.Area < largestArea) continue;

                largestArea = area.Area;
                origin = Rhino.Geometry.AreaMassProperties.Compute(brep).Centroid;

                double u; double v;
                brep.ClosestPoint(origin, out u, out v);
                normal = brep.NormalAt(u, v); //set the normal of the largest surface
                NoPlanarSurfacesFound = false;
            }

            if (NoPlanarSurfacesFound)
            {
                foreach (BrepFace brep in B.Faces)
                {
                    Rhino.Geometry.AreaMassProperties area = Rhino.Geometry.AreaMassProperties.Compute(brep);
                    if (area.Area <= largestArea) continue;

                    largestArea = area.Area;
                    origin = Rhino.Geometry.AreaMassProperties.Compute(brep).Centroid;

                    double u; double v;
                    brep.ClosestPoint(origin, out u, out v);
                    normal = brep.NormalAt(u, v); //set the normal of the largest surface
                    NoPlanarSurfacesFound = false;
                }
            }

            //get forward direction vector from longest line of brep
            forward = new Vector3d(0, 0, 0);
            double longestLength = 0;
            foreach (Curve crv in B.Edges)
            {
                //we are only interested in linear curves
                if (!crv.IsLinear()) continue;

                double length = crv.GetLength();
                if (length < longestLength) continue;

                longestLength = length;
                forward = crv.PointAtEnd - crv.PointAtStart;
            }

            //contruct orientation plane from normal and forward vector
            plane = new Plane(origin, normal);
            forward.Transform(Rhino.Geometry.Transform.PlanarProjection(plane));
            forward.Unitize();
            double angle = Vector3d.VectorAngle(plane.YAxis, forward, plane);
            plane.Rotate(angle, normal);

            //create bounding box
            Box worldBox;
            BoundingBox box = B.GetBoundingBox(plane, out worldBox);

            return worldBox;
        }

        public List<System.Guid> SelectObjectsByLayer(RhinoDoc doc, Rhino.DocObjects.Layer layer)
        {
            List<System.Guid> guids = new List<System.Guid>();

            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer);
            foreach (Rhino.DocObjects.RhinoObject rhobj in rhobjs)
            {
                doc.Objects.Select(rhobj.Id, true, true, true);
                guids.Add(rhobj.Id);
            }

            return guids;
        }

        public Mesh MeshCube(Point3d A, Point3d B, Point3d C, Point3d D, Point3d E, Point3d F, Point3d G, Point3d H)
        {
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            mesh.Vertices.Add(A); //0
            mesh.Vertices.Add(B); //1
            mesh.Vertices.Add(C); //2
            mesh.Vertices.Add(D); //3
            mesh.Vertices.Add(E); //4
            mesh.Vertices.Add(F); //5
            mesh.Vertices.Add(G); //6
            mesh.Vertices.Add(H); //7

            mesh.Faces.AddFace(7, 6, 5, 4);
            mesh.Faces.AddFace(1, 0, 4, 5);
            mesh.Faces.AddFace(2, 1, 5, 6);
            mesh.Faces.AddFace(3, 2, 6, 7);
            mesh.Faces.AddFace(0, 3, 7, 4);

            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }

        public GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ImportCSV(string filepath, string delimiter)
        {
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>
                treeArray = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();

            string[] text = System.IO.File.ReadAllLines(filepath);

            for (int i = 0; i < text.Length; i++)
            {
                string[] parts = text[i].Split(delimiter[0]); //split row
                for (int j = 0; j < parts.Length; j++)
                {
                    Grasshopper.Kernel.Data.GH_Path ghpath = new Grasshopper.Kernel.Data.GH_Path(i);
                    Grasshopper.Kernel.Types.GH_String cell = new Grasshopper.Kernel.Types.GH_String(parts[j]);
                    treeArray.Append(cell, ghpath);
                }
            }
            return treeArray;
        }

        public bool Write(string filepath, string[] data)
        {
            //string filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            //string folder = System.IO.Path.GetDirectoryName(filepath);
            //filepath = folder + "\\" + filename + ".csv";

            //writefile
            System.IO.File.WriteAllText(filepath, string.Join(Environment.NewLine, data), System.Text.Encoding.UTF8);
            return true;
        }

        // !!!NOTE: IF MODIFYING SCOPES!!!,
        // delete your previously saved credentials at ~/.credentials/sheets.googleapis.com-dotnet-quickstart.json
        private static string[] Scopes = { SheetsService.Scope.Spreadsheets };

        private static string ApplicationName = "Yung GH";

        public GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> ReadGoogleSpreadsheet(string authentication, String spreadsheetId, string tab)
        {
            //credential
            UserCredential credential;
            using (var stream =
                new FileStream(authentication, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Debug.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String range = tab;// + "!A2:E";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);

            //Read the cells in a spreadsheet:
            ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;
            if (values == null || values.Count == 0)
            {
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No data found in spreadsheet.");
                return null;
            }

            //convert data
            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>
                treeArray = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo>();
            int i = 0;
            foreach (var row in values)
            {
                for (int j = 0; j < row.Count; j++)
                {
                    Grasshopper.Kernel.Data.GH_Path ghpath = new Grasshopper.Kernel.Data.GH_Path(i);
                    Grasshopper.Kernel.Types.GH_String cell = new Grasshopper.Kernel.Types.GH_String(row[j].ToString());
                    treeArray.Append(cell, ghpath);
                }
                i++;
            }
            return treeArray;
        }

        public bool WriteGoogleSpreadsheet(string authentication, String spreadsheetId, string tab, GH_Structure<Grasshopper.Kernel.Types.GH_String> data)
        {
            //credential
            UserCredential credential;
            using (var stream =
                new FileStream(authentication, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None//,
                //new FileDataStore(credPath, true)
                ).Result;
                //Debug.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //Convert Data
            IList<IList<object>> convertedData = new List<IList<object>>();
            foreach (IList<Grasshopper.Kernel.Types.GH_String> list in data.Branches)
            {
                IList<object> row = new List<object>();
                foreach (Grasshopper.Kernel.Types.GH_String text in list)
                {
                    row.Add(text.ToString());
                }
                convertedData.Add(row);
            }

            //make sure the document exists
            SpreadsheetsResource.GetRequest getRequest = service.Spreadsheets.Get(spreadsheetId);
            Spreadsheet resource = getRequest.Execute();

            //make sure the sheet exists, if not add it.
            bool sheetExists = false;
            foreach (Sheet sheet in resource.Sheets) { if (sheet.Properties.Title == tab) { sheetExists = true; } }
            if (!sheetExists)
            {
                AddNewTab(service, spreadsheetId, tab);
            }

            //Writing
            ValueRange Update = new ValueRange();
            Update.Values = convertedData;
            Update.Range = tab;

            //UPDATE
            SpreadsheetsResource.ValuesResource.UpdateRequest Updaterequest = service.Spreadsheets.Values.Update(Update, spreadsheetId, tab);
            Updaterequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            UpdateValuesResponse Traderesult = Updaterequest.Execute();
            return true;
        }

        private void AddNewTab(SheetsService service, string spreadsheetId, string sheetName)
        {
            var addSheetRequest = new AddSheetRequest();
            addSheetRequest.Properties = new SheetProperties();
            addSheetRequest.Properties.Title = sheetName;
            BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
            batchUpdateSpreadsheetRequest.Requests = new List<Request>();
            batchUpdateSpreadsheetRequest.Requests.Add(new Request
            {
                AddSheet = addSheetRequest
            });

            var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);

            batchUpdateRequest.Execute();
        }

        public Dictionary<string, string> ReadObjectAttributes(Rhino.DocObjects.RhinoObject obj)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            Rhino.DocObjects.ObjectAttributes objattributes = new Rhino.DocObjects.ObjectAttributes();
            objattributes = obj.Attributes;

            NameValueCollection keyValues = objattributes.GetUserStrings();
            foreach (string key in keyValues)
            {
                attributes.Add(key, keyValues[key]);
            }
            return attributes;
        }

        public Rhino.DocObjects.ObjectAttributes WriteObjectAttributes(object obj, string name, List<string> key, List<string> val, bool clean)
        {
            Rhino.DocObjects.ObjectAttributes objattributes = new Rhino.DocObjects.ObjectAttributes();

            if (obj is System.Guid || obj is GH_Guid)
            {
                System.Guid guid = System.Guid.Empty;
                if (obj is System.Guid)
                    guid = (System.Guid)obj;
                else
                {
                    GH_Guid gh_guid = (GH_Guid)obj;
                    guid = gh_guid.Value;
                }
                Rhino.DocObjects.RhinoObject rhinoObject = Rhino.RhinoDoc.ActiveDoc.Objects.Find(guid);

                //get existing attributes if we are writing clean attributes (meaning we delete the old name and attributes)
                if (!clean)
                    objattributes = rhinoObject.Attributes;

                //add name if it isn't null or empty
                if (!String.IsNullOrEmpty(name) && name != "")
                    objattributes.Name = name;

                //add attributes
                for (int i = 0; i < key.Count; i++)
                {
                    objattributes.SetUserString(key[i], val[i]);
                }

                //update object in rhino
                rhinoObject.Attributes = objattributes;
                rhinoObject.CommitChanges();
            }
            else
            {
                //set object attributes
                objattributes.Name = name;
                for (int i = 0; i < key.Count; i++)
                {
                    objattributes.SetUserString(key[i], val[i]);
                }
            }

            return objattributes;
        }

        private const string layerDelimiter = "::";

        public Rhino.DocObjects.Layer LayerByFullPath(string layerPath)
        {
            Rhino.DocObjects.Layer layer = null;

            foreach (Rhino.DocObjects.Layer tempLayer in RhinoDoc.ActiveDoc.Layers)
            {
                if (tempLayer.FullPath != layerPath) continue;

                layer = tempLayer;
                break;
            }

            return layer;
        }

        public void DeleteObjectsOnLayer(RhinoDoc doc, Rhino.DocObjects.Layer layer)
        {
            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer);
            foreach (Rhino.DocObjects.RhinoObject rhobj in rhobjs)
            {
                doc.Objects.Delete(rhobj, true, true);
            }
        }

        public Rhino.DocObjects.Layer CreateModify(RhinoDoc doc, string layerPath, System.Drawing.Color color,
          bool locked, string lineTypeName,
          string materialName, bool onOff,
          System.Drawing.Color printColor, double printWidth, bool delete)
        {
            //get layer if already existing
            Rhino.DocObjects.Layer layer = LayerByFullPath(layerPath);

            //if we want to delete the layer
            if (delete)
            {
                if (layer == null) return null;

                //make sure it's not the current layer
                if (doc.Layers.CurrentLayer.Index == layer.Index)
                    doc.Layers.SetCurrentLayerIndex(0, true); //TODO: if they are deleting this layer index it will throw an error
                DeleteObjectsOnLayer(doc, layer);
                doc.Layers.Delete(layer.Index, true);
                return null;
            }

            //if the target layer does not exist, create it
            if (layer == null)
            {
                //if it is a single layer path
                if (!layerPath.Contains(layerDelimiter))
                {
                    layer = new Rhino.DocObjects.Layer { Id = Guid.NewGuid(), Name = layerPath, Index = doc.Layers.Count };
                    int index = doc.Layers.Add(layer);
                }
                //if the path has tree depth
                else
                {
                    string[] fullPath = layerPath.Split(new string[] { layerDelimiter }, StringSplitOptions.None);
                    string parent = "";
                    foreach (string nextPath in fullPath)
                    {
                        layer = new Rhino.DocObjects.Layer { Id = Guid.NewGuid(), Name = nextPath, Index = doc.Layers.Count };
                        if (parent != "")
                        {
                            string parentName = parent.Substring(2, parent.Length - 2); //remove the delimiter from the front
                            Rhino.DocObjects.Layer parentLayer = LayerByFullPath(parentName);
                            layer.ParentLayerId = parentLayer.Id;
                        }

                        int index = doc.Layers.Add(layer);
                        parent += layerDelimiter + nextPath;
                    }
                }

                //get layer after it's been added
                layer = LayerByFullPath(layerPath);
            }

            if (layer == null) return layer;

            //layer color
            if (color != null) layer.Color = color;

            //layer is locked
            layer.IsLocked = locked;

            //layer visibility
            layer.IsVisible = onOff;

            //linetype
            Rhino.DocObjects.Linetype lineType = doc.Linetypes.FindName(lineTypeName);
            if (lineType != null) layer.LinetypeIndex = lineType.LinetypeIndex;

            //layer material
            layer.RenderMaterialIndex = doc.Materials.Find(materialName, true);

            //print width
            layer.PlotWeight = printWidth;

            //print color
            layer.PlotColor = printColor;

            return layer;
        }

        public void BakeGeometry(List<GeometryBase> geometries, List<string> layers)
        {
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            int layer = -1;
            string name = "";

            for (int i = 0; i < geometries.Count; i++)
            {
                //find layer
                if (i < layers.Count)
                {
                    layer = GetLayerIndex(doc, layers[i]);
                }

                //create attributes
                Rhino.DocObjects.ObjectAttributes attributes = new Rhino.DocObjects.ObjectAttributes();
                attributes.LayerIndex = layer;
                attributes.Name = name;
                //attributes.ColorSource = ObjectColorSource.ColorFromObject;
                //attributes.ObjectColor = Color.Black;

                //bake geometry
                doc.Objects.Add(geometries[i], attributes);
            }
        }

        public int GetLayerIndex(Rhino.RhinoDoc doc, string layer)
        {
            string layer_name = layer.Trim();

            //does layer name contain data?
            if (string.IsNullOrEmpty(layer_name))
            {
                Rhino.RhinoApp.WriteLine("Layer name cannot be blank.");
                return -1;
            }

            // Is the layer name valid?
            if (!Rhino.DocObjects.Layer.IsValidName(layer_name))
            {
                Rhino.RhinoApp.WriteLine(layer_name + " is not a valid layer name.");
                return -1;
            }

            // Does a layer with the same name already exist?            
            Rhino.DocObjects.Layer layer_object = doc.Layers.FindName(layer_name, -1);
            int layer_index = -1;
            if (layer_object != null)
            {
                layer_index = layer_object.Index;
            }
            //int layer_index = doc.Layers.Find(layer_name, true);
            if (layer_index >= 0)
            {
                Rhino.RhinoApp.WriteLine("A layer with the name {0} already exists.", layer_name);
                return layer_index;
            }

            // Add a new layer to the document
            layer_index = doc.Layers.Add(layer_name, System.Drawing.Color.Black);
            if (layer_index < 0)
            {
                Rhino.RhinoApp.WriteLine("Unable to add {0} layer.", layer_name);
                return -1;
            }
            Rhino.RhinoApp.WriteLine("{0} layer added.", layer_name);
            return layer_index;
        }

        public void Select(RhinoDoc doc, List<System.Guid> guids)
        {
            foreach (System.Guid guid in guids)
                doc.Objects.Select(guid, true, true, true);
        }

        public void ExportModel(string filepath)
        {
            string scriptExport = string.Format("_-Export \"{0}\" _Enter", filepath);
            RhinoApp.RunScript(scriptExport, false);
        }

        public List<System.Guid> ImportModel(string filepath)
        {
            List<System.Guid> importedGuids = new List<System.Guid>();

            //file exist guard statement
            if (!File.Exists(filepath)) return importedGuids;

            //rhino okay file type guard statement
            if (!SupportedImportFileTypes.Contains(Path.GetExtension(filepath))) return importedGuids;

            //get existing objects
            List<System.Guid> existingGuids = GetGuids();

            string import = string.Format("_-Import \"{0}\" _Enter", filepath);
            Rhino.RhinoApp.RunScript(import, false);

            importedGuids = GetGuids();

            //remove guids existing before the import
            importedGuids.RemoveAll(x => existingGuids.Contains(x));

            return importedGuids;
        }

        public void ResetButtonComponents(object sender, GH_SolutionEventArgs e)
        {
            e.Document.SolutionEnd -= ResetButtonComponents;

            //GH_Document doc = this.OnPingDocument();
            /*/
            foreach (IGH_DocumentObject obj in doc.Objects) //Search all components in the Canvas
            {
                //Print(obj.Name);
                if (obj.Name != "Button") continue;

                //obj.ExpireSolution(true); //recompute this component, this is a way to restart upstream
                //obj.ExpirePreview(true);
            }
            //*/
        }

        public string TestSurfaceDevelopability(Surface surface, out int type)
        {
            //https://discourse.mcneel.com/t/verifying-developable-surfaces/73594
            //https://discourse.mcneel.com/t/ruling-line-from-edge-curves-twist-check/73952

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //planar
            if (surface.IsPlanar(tolerance)) { type = 0; return "planar"; }

            //cylinder
            if (surface.IsCylinder(tolerance)) { type = 1; return "cylinder"; }

            //conic
            if (surface.IsCone(tolerance)) { type = 2; return "conic"; }

            //spheric
            if (surface.IsSphere(tolerance) || surface.IsTorus(tolerance)) { type = 4; return "double curved"; }

            //ruled surface

            Rhino.Geometry.Unroller unroll = null;
            Rhino.Geometry.Surface srf = surface;
            if (srf != null)
                unroll = new Rhino.Geometry.Unroller(srf);

            Rhino.Geometry.Brep brep = surface.ToBrep();

            if (unroll == null)
            {
                type = 4;
                return "double curved";
            }
            else
            {
                type = 3;
                return "ruled surface";
            }
        }

        public List<Object> FindExtremums(Brep brep, List<Vector3d> directions, bool minmax)
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            List<Object> extremums = new List<Object>();
            //Rhino.Geometry.GeometryBase
            foreach (Vector3d normal in directions)
            {
                Plane plane = new Plane(new Point3d(0, 0, 0), normal);

                //create bounding box
                Box worldBox;
                BoundingBox box = brep.GetBoundingBox(plane, out worldBox);

                //using the world box corners, we create top or bottom planes
                Point3d[] corners = worldBox.GetCorners();
                Plane intersection = new Plane(corners[0], corners[1], corners[2]); //bottom plane
                if (!minmax)
                    intersection = new Plane(corners[4], corners[5], corners[6]); //top plane

                Curve[] crvs;
                Point3d[] pts;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, intersection, 0, out crvs, out pts)) { continue; }

                foreach (Curve crv in crvs)
                    extremums.Add(crv);
                foreach (Point3d pt in pts)
                    extremums.Add(pt);

                if (crvs.Length > 0 || pts.Length > 0) continue;

                //if no curve intersections were found, we need to get all the edges of the brep and check for intersections.
                List<Point3d> Xpts = new List<Point3d>();
                foreach (Curve edg in brep.Edges)
                {
                    Rhino.Geometry.Intersect.CurveIntersections Xcrv = Rhino.Geometry.Intersect.Intersection.CurvePlane(edg, intersection, tolerance);
                    if (Xcrv == null) continue;

                    for (int i = 0; i < Xcrv.Count; i++)
                    {
                        Rhino.Geometry.Intersect.IntersectionEvent crvX = Xcrv[i];
                        Xpts.Add(crvX.PointA);
                    }
                }
                Point3d[] culledXpts = Point3d.CullDuplicates(Xpts, tolerance);
                foreach (Point3d pt in culledXpts)
                    extremums.Add(pt);
            }

            return extremums;
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="P"> List of planes to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        public List<Brep> SafeSplit(List<Brep> breps, List<Plane> P, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (P.Count == 0) return breps;

            Plane plane = P[0];
            P.RemoveAt(0);
            foreach (Brep brep in breps)
            {
                Curve[] intersections;
                Point3d[] pt;
                if (!Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, plane, tolerance, out intersections, out pt)) { splits.Add(brep); continue; }

                //if the intersection failed, we keep the original brep
                if (intersections.Length == 0) { splits.Add(brep); continue; }

                Brep[] test = brep.Split(intersections, tolerance); //it will always only be one intersection at a time

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (P.Count != 0)
            {
                splits = SafeSplit(splits, P, tolerance);
            }

            return splits;
        }

        /// <summary>
        /// Recursively splits a list of breps with a list of planes.
        /// </summary>
        /// <param name="breps"> List of breps to split.</param>
        /// <param name="C"> List of curves to split brep with.</param>
        /// <param name="tolerance"> Tolerance for splitting.</param>
        /// <returns>List of split breps</returns>
        public List<Brep> SafeSplit(List<Brep> breps, List<Curve> C, double tolerance)
        {
            List<Brep> splits = new List<Brep>();

            //guard statement in case not list is supplied
            if (C.Count == 0) return breps;

            Curve crv = C[0];
            C.RemoveAt(0);
            foreach (Brep brep in breps)
            {
                Brep[] test = brep.Split(new List<Curve>() { crv }, tolerance); //it will always only be one intersection at a time

                //if the split failed, we keep the original brep
                if (test.Length == 0) { splits.Add(brep); continue; }

                //we add each brep output to the list for recursion
                foreach (Brep b in test) { splits.Add(b); }
            }

            if (C.Count != 0)
            {
                splits = SafeSplit(splits, C, tolerance);
            }

            return splits;
        }

        /// <summary>
        /// Checks which breps a list of points is touching
        /// </summary>
        /// <param name="breps">List of breps for testing point touch</param>
        /// <param name="pts">List of points to check if brep is touching</param>
        /// <param name="tolerance"> Tolerance for point check</param>
        /// <returns>The indices of the breps touched by points</returns>
        public List<int> BrepPointCheck(List<Brep> breps, List<Point3d> pts, double tolerance)
        {
            List<int> indices = new List<int>();

            foreach (Point3d pt in pts)
            {
                for (int i = 0; i < breps.Count; i++)
                {
                    Brep brep = breps[i];
                    Point3d cp = brep.ClosestPoint(pt);
                    double dist = pt.DistanceTo(cp);
                    if (dist <= tolerance)
                    {
                        indices.Add(i);
                    }
                }
            }
            return indices;
        }

        public List<System.Guid> GetGuids()
        {
            List<System.Guid> guids = new List<System.Guid>();
            foreach (Rhino.DocObjects.RhinoObject obj in Rhino.RhinoDoc.ActiveDoc.Objects)
            {
                guids.Add(obj.Id);
            }
            return guids;
        }

        public List<string> SupportedImportFileTypes = new List<string>(){
      ".3dm",".3dmbak",".rws",".3mf",".3ds",".amf",".ai",
      ".dwg",".dxf",".x",".e57",".dst",".exp",".eps",".off",
      ".gf",".gft",".gts",".igs",".iges",".lwo",".dgn",".fbx",
      ".scn",".obj",".pdf",".ply",".asc",".csv",".xyz",".cgo_ascii",
      ".cgo_asci",".pts",".txt",".raw",".m",".svg",".skp",".slc",
      ".sldprt",".sldasm",".stp",".step",".stl",".vda",".wrl",
      ".vrml",".vi",".gdf",".zpr"
      };

        public List<string> SupportedExportFileTypes = new List<string>(){
      ".3dm",".3dmbak",".rws",".3mf",".3ds",".amf",".ai",
      ".dwg",".dxf",".x",
      ".gf",".gft",".gts",".igs",".iges",".lwo",".dgn",".fbx",
      ".obj",".pdf",".ply",".csv",
      ".txt",".raw",".m",".svg",".skp",".slc",
      ".stp",".step",".stl",".vda",".wrl",
      ".vrml",".vi",".gdf",".zpr",
      ".dae",".cd",".emf",".pm",".kmz",".udo",".x_t",".rib",".wmf",".x3dv",".xaml",".xgl"
      };
    }
}