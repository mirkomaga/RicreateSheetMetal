using Inventor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Point = Inventor.Point;

namespace RicreateSheetMetal
{
    class RicompongoLamiera
    {
        public static Inventor.Application iApp = null;
        public static void main(string path, ToolStripProgressBar pb1, ToolStripStatusLabel tstl1, ListView lv1)
        {
            // ! Istanza inventor
            getIstance();

            // ! Tutti gli ipt dentro la folder
            // ? oDoc = (PartDocument) iApp.ActiveDocument;
            string[] listFiles = System.IO.Directory.GetFiles(@path, "*.ipt");

            int counter = 0;

            pb1.Minimum = 0;
            pb1.Maximum = listFiles.Length;

            // ! Ciclo la lista ipt dentro la folder
            foreach (string file in listFiles)
            {
                counter++;

                pb1.Value = counter;

                if (System.IO.Path.GetExtension(file) == ".ipt")
                {
                    tstl1.Text = System.IO.Path.GetFileName(file);

                    // ! Apro il documento
                    PartDocument oDoc = (PartDocument)iApp.Documents.Open(@file);

                    // ! Imposto SheetMetalDocument
                    setSheetMetalDocument(oDoc);

                    // ! Imposto thickness
                    setThickness(oDoc);

                    // ! Elimino raggiature
                    List<string> faceCollToKeep = deleteFillet(oDoc);
                    if (faceCollToKeep == null)
                    {

                        ListViewItem item1 = new ListViewItem(System.IO.Path.GetFileName(file), 0);
                        item1.SubItems.Add("Errore nell'eliminazione delle raggiature");
                        lv1.Items.AddRange(new ListViewItem[] { item1 });

                        //oDoc.Close();
                        //continue;
                    }
                    else
                    {
                        // ! Elimino facce
                        deleteFace(oDoc, faceCollToKeep);

                        // ! Creo Profilo
                        bool profStatus = createProfile(oDoc);
                        if (!profStatus)
                        {

                            ListViewItem item1 = new ListViewItem(System.IO.Path.GetFileName(file), 0);
                            item1.SubItems.Add("Errore nella creazione profilo");
                            lv1.Items.AddRange(new ListViewItem[] { item1 });

                            //oDoc.Close();
                            continue;
                        }

                        // ! Creo Raggiature
                        createFillet(oDoc);

                    }

                    // ! Cerco le lavorazioni
                    // IDictionary<Face, List<Lavorazione>> lavorazione = detectLavorazioni(oDoc);

                    // ! Creo sketch lavorazioni
                    // List<string>  nomeSketch = createSketchLavorazione(oDoc, lavorazione);

                    // ! Elimino con direct le lavorazioni
                    // deleteLavorazione(oDoc);

                    // ! Cut lavorazione
                    // createCutLavorazione(oDoc, nomeSketch);

                    // ! Aggiungo piano nel mezzo
                    WorkPlane oWpReference = addPlaneInTheMiddleOfBox(oDoc);

                    // ! Aggiungo proiezione cut
                    bool projCutStatus = addProjectCut(oDoc, oWpReference);
                    if (!projCutStatus)
                    {
                        ListViewItem item1 = new ListViewItem(System.IO.Path.GetFileName(file), 0);
                        item1.SubItems.Add("Errore nella creazione della proiezione cut");
                        lv1.Items.AddRange(new ListViewItem[] { item1 });

                        //oDoc.Close();
                        continue;
                    }

                    // ! Coloro lato bello
                    setTexture(oDoc);

                    // ! Salvo il documento
                    oDoc.Save();

                    // ! Faccio lo sviluppo
                    bool sviluppoLamStatus = sviluppoLamiera(oDoc);
                    if (!sviluppoLamStatus)
                    {
                        ListViewItem item1 = new ListViewItem(System.IO.Path.GetFileName(file), 0);
                        item1.SubItems.Add("Errore nella creazione dello sviluppo");
                        lv1.Items.AddRange(new ListViewItem[] { item1 });
                    }

                    // ! Chiudo il documento
                    // ? oDoc.Close(true);
                    oDoc.Close();
                }
            }

            MessageBox.Show("Procedimento completato", "Creator Sheet Metal");

        }
        public static void saveAllAsDxf(string path)
        {
            // ! Istanza inventor
            getIstance();

            // ! Tutti gli ipt dentro la folder
            // ? oDoc = (PartDocument) iApp.ActiveDocument;
            string[] listFiles = System.IO.Directory.GetFiles(@path, "*.ipt");

            int counter = 0;

            // ! Ciclo la lista ipt dentro la folder
            foreach (string file in listFiles)
            {
                counter++;

                if (System.IO.Path.GetExtension(file) == ".ipt")
                {
                    // ! Apro il documento
                    PartDocument oDoc = (PartDocument)iApp.Documents.Open(@file);

                    bool dxfStatus = saveDxf(path, oDoc);

                    if (!dxfStatus)
                    {
                        Console.WriteLine("dasdsadsdas");
                    }

                    oDoc.Close(true);
                } 
            }
        }
        // ! Creo lo sviluppo della lamiera
        public static bool sviluppoLamiera(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            try
            {
                oCompDef.Unfold();
                return true;
            }
            catch
            {
                return false;
            }
        }
        // ! Controlla se inventor è già aperto, se no lo apre
        public static void getIstance()
        {
            try
            {
                iApp = (Inventor.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Inventor.Application");
                iApp.Visible = true;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                iApp = (Inventor.Application)System.Activator.CreateInstance(System.Type.GetTypeFromProgID("Inventor.Application"));
                iApp.Visible = true;
            }
        }
        // ! Imposta documento ipt di tipo SheetMetal
        public static void setSheetMetalDocument(PartDocument oDoc)
        {

            if (oDoc.SubType != "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}")
            {
                oDoc.SubType = "{9C464203-9BAE-11D3-8BAD-0060B0CE6BB4}";
            }
        }
        // ! Imposta thickness
        public static void setThickness(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            Face biggestFace = getBiggestFace(oDoc);
            
            double thikness = Math.Round(getDistanceFromFace(biggestFace) * 100)/100;

            string toSelect = null;

            switch (thikness)
            {
                case 0.6:
                    toSelect = "Aluminium THK 0.6mm";
                    break;
                case 0.8:
                    toSelect = "Aluminium THK 0.8mm";
                    break;
                case 1.0:
                    toSelect = "Aluminium THK 1.0mm";
                    break;
                case 1.2:
                    toSelect = "Aluminium THK 1.2mm";
                    break;
                case 1.5:
                    toSelect = "Aluminium THK 1.5mm";
                    break;
                case 2.0:
                    toSelect = "Aluminium THK 2.0mm";
                    break;
                case 2.5:
                    toSelect = "Aluminium THK 2.5mm";
                    break;
                case 3.0:
                    toSelect = "Aluminium THK 3.0mm";
                    break;
                case 4.0:
                    toSelect = "Aluminium THK 4.0mm";
                    break;
                case 5.0:
                    toSelect = "Aluminium THK 5.0mm";
                    break;
                default:
                    //toSelect = "Default_mm";
                    Console.WriteLine("SONO ENTRATO IN DEFAULT");
                    break;
            }

            if (!string.IsNullOrEmpty(toSelect))
            {
                SheetMetalStyle oStyle = oCompDef.SheetMetalStyles[toSelect];
                oStyle.Activate();
            }
            else
            {
                throw new Exception("Nessuno spessore trovato");
            }
        }
        // ! Colora entità con highlight set
        public static void coloroEntita(PartDocument oDoc, byte r, byte g, byte b, dynamic e)
        {
            HighlightSet oHS2 = oDoc.HighlightSets.Add();
            oHS2.Color = iApp.TransientObjects.CreateColor(r, g, b);
            oHS2.AddItem(e);
        }
        // ! Elimina tutti i fillet
        public static List<string> deleteFillet(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;
            
            List<string> faceCollToKeep = new List<string>();

            if (oCompDef.Bends.Count > 0)
            {
                Bend oBend = oCompDef.Bends[1];

                FaceCollection oFaceColl = oBend.FrontFaces[1].TangentiallyConnectedFaces;
                oFaceColl.Add(oBend.FrontFaces[1]);

                foreach (Face oFace in oFaceColl)
                {
                    faceCollToKeep.Add(oFace.InternalName);
                }

                NonParametricBaseFeature oBaseFeature = oCompDef.Features.NonParametricBaseFeatures[1];

                oBaseFeature.Edit();

                SurfaceBody basebody = oBaseFeature.BaseSolidBody;

                ObjectCollection oColl = iApp.TransientObjects.CreateObjectCollection();

                foreach (Face f in basebody.Faces)
                {
                    if (faceCollToKeep.Contains(f.InternalName))
                    {
                        if (f.SurfaceType == SurfaceTypeEnum.kCylinderSurface)
                        {
                            oColl.Add(f);
                        }
                    }
                }
                try
                {
                    oBaseFeature.DeleteFaces(oColl);
                    oBaseFeature.ExitEdit();
                }
                catch
                {
                    oBaseFeature.ExitEdit();
                    return null;
                }
            }

            return faceCollToKeep;
        }
        // ! Elimina facce
        public static void deleteFace(PartDocument oDoc, List<string> faceCollToKeep)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            FaceCollection faceCollToDelete = iApp.TransientObjects.CreateFaceCollection();

            foreach (Face oFace in oCompDef.SurfaceBodies[1].Faces)
            {
                if (!faceCollToKeep.Contains(oFace.InternalName))
                {
                    faceCollToDelete.Add(oFace);
                }
            }

            PartFeatures oFeat = oCompDef.Features;
            oFeat.DeleteFaceFeatures.Add(faceCollToDelete);
        }
        // ! Crea il profilo della superficie
        public static bool createProfile(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            FaceCollection oFaceColl = iApp.TransientObjects.CreateFaceCollection();

            foreach (Face f in oCompDef.SurfaceBodies[1].Faces)
            {
                oFaceColl.Add(f);
            }

            PartFeatures oFeat = oCompDef.Features;

            try
            {
                oFeat.ThickenFeatures.Add(oFaceColl, Math.Round(oCompDef.Thickness.Value * 100) / 100, PartFeatureExtentDirectionEnum.kNegativeExtentDirection, PartFeatureOperationEnum.kJoinOperation, false);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // ! Ricrea le raggiature
        public static void createFillet(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            SheetMetalFeatures sFeatures = (SheetMetalFeatures)oCompDef.Features;

            foreach (Edge oEdge in oCompDef.SurfaceBodies[1].ConcaveEdges)
            {
                int tmpCount = oCompDef.SurfaceBodies[1].ConcaveEdges.Count;

                coloroEntita(oDoc, 255,0,0, oEdge);

                try
                {
                    EdgeCollection oBendEdges = iApp.TransientObjects.CreateEdgeCollection();

                    oBendEdges.Add(oEdge);

                    BendDefinition oBendDef = sFeatures.BendFeatures.CreateBendDefinition(oBendEdges);

                    BendFeature oBendFeature = sFeatures.BendFeatures.Add(oBendDef);

                    //if (tmpCount != oCompDef.SurfaceBodies[1].ConcaveEdges.Count)
                    //{
                    createFillet(oDoc);
                    break;
                    //}
                }
                catch { }
            }
        }
        // ! Rivela le lavorazioni
        public static IDictionary<Face, List<Lavorazione>> detectLavorazioni(PartDocument oDoc)
        {
            IDictionary<Face, List<Lavorazione>> result = new Dictionary<Face, List<Lavorazione>>();

            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            FaceCollection oFaceColl = oCompDef.Bends[1].FrontFaces[1].TangentiallyConnectedFaces;
            oFaceColl.Add(oCompDef.Bends[1].FrontFaces[1]);

            foreach (Face f in oFaceColl)
            {
                if (f.EdgeLoops.Count > 1)
                {
                    List<Lavorazione> lavorazione = IdentificazioneEntita.main(f.EdgeLoops, iApp);

                    if (lavorazione.Count > 0)
                    {
                        result.Add(f, lavorazione);
                    }
                }
            }

            return result;
        }
        // ! Crea sketch delle lavorazioni
        public static List<string> createSketchLavorazione(PartDocument oDoc, IDictionary<Face, List<Lavorazione>> lavorazione)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            List<string> result = new List<string>();

            int index = 1;
            foreach (var x in lavorazione) 
            {
                Face oFace = x.Key;
                List<Lavorazione> lavList = x.Value;
                
                PlanarSketch oSketch = oCompDef.Sketches.Add(oFace, false);

                foreach (Lavorazione lav in lavList)
                {
                    foreach(Edge oEdge in lav.oEdgeColl_)
                    {
                        oSketch.AddByProjectingEntity(oEdge);
                        oSketch.Visible = false;
                    }
                }
                result.Add(oSketch.Name);
            }

            return result;
        }
        // ! Elimina le lavorazioni
        public static void deleteLavorazione(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            NonParametricBaseFeature oBaseFeature = oCompDef.Features.NonParametricBaseFeatures[1];
            oBaseFeature.Edit();

            SurfaceBody basebody = oBaseFeature.BaseSolidBody;

            foreach (Face f in basebody.Faces)
            {
                try
                {
                    string nameFaceRif = f.InternalName;

                    ObjectCollection oFaceColl = iApp.TransientObjects.CreateObjectCollection();

                    if (f.EdgeLoops.Count > 1)
                    {
                        foreach (EdgeLoop oEdgeLoop in f.EdgeLoops)
                        {
                            string lavorazione = IdentificazioneEntita.whois(oEdgeLoop.Edges);

                            if (!string.IsNullOrEmpty(lavorazione))
                            {
                                foreach (Face oFaceLav in oEdgeLoop.Edges[1].Faces)
                                {
                                    string nameFaceLav = oFaceLav.InternalName;

                                    if (nameFaceLav != nameFaceRif)
                                    {
                                        oFaceColl.Add(oFaceLav);

                                        foreach (Face oFaceTan in oFaceLav.TangentiallyConnectedFaces)
                                        {
                                            oFaceColl.Add(oFaceTan);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (oFaceColl.Count > 0)
                    {
                        try
                        {
                            oBaseFeature.DeleteFaces(oFaceColl);
                            oBaseFeature.ExitEdit();
                            deleteLavorazione(oDoc);
                        }
                        catch
                        {

                        }
                    }
                }
                catch
                {

                }
            }
            oBaseFeature.ExitEdit();
        }
        // ! Crea la lavorazione dallo sketch
        public static void createCutLavorazione(PartDocument oDoc, List<string> nomeSketch)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            foreach (string nameS in nomeSketch)
            {
                try
                {
                    PlanarSketch oSketch = oCompDef.Sketches[nameS];

                    SheetMetalFeatures oSheetMetalFeatures = (SheetMetalFeatures)oCompDef.Features;

                    Profile oProfile = oSketch.Profiles.AddForSolid();

                    CutDefinition oCutDefinition = oSheetMetalFeatures.CutFeatures.CreateCutDefinition(oProfile);

                    //oCutDefinition.SetThroughAllExtent(PartFeatureExtentDirectionEnum.kNegativeExtentDirection);

                    CutFeature oCutFeature = oSheetMetalFeatures.CutFeatures.Add(oCutDefinition);
                }
                catch
                {
                    throw new Exception("Nome sketch non esiste: " + nameS);
                }
            }
        }
        // ! Aggiunge il piano in mezzo alla lamiera
        public static WorkPlane addPlaneInTheMiddleOfBox(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oComp = (SheetMetalComponentDefinition) oDoc.ComponentDefinition;

            try
            {
                return oComp.WorkPlanes["Manual"];
            }
            catch { }

            Box oRb = oComp.SurfaceBodies[1].RangeBox;

            TransientBRep oTransientBRep = iApp.TransientBRep;

            SurfaceBody oBody = oTransientBRep.CreateSolidBlock(oRb);

            NonParametricBaseFeature oBaseFeature = oComp.Features.NonParametricBaseFeatures.Add(oBody);

            FaceCollection oFaceColl = iApp.TransientObjects.CreateFaceCollection();

            foreach (Face f in oBaseFeature.SurfaceBodies[1].Faces)
            {
                WorkPlane tmpWp = oComp.WorkPlanes.AddByPlaneAndOffset(f, 0);

                //if (tmpWp.Plane.IsParallelTo[oComp.WorkPlanes[1].Plane])
                if (tmpWp.Plane.IsParallelTo[oComp.WorkPlanes[3].Plane])
                    {
                    oFaceColl.Add(f);
                }

                tmpWp.Delete();
            }

            WorkPlane wpWork = null;
            if (oFaceColl.Count >= 2)
            {
                WorkPlane wp1 = oComp.WorkPlanes.AddByPlaneAndOffset(oFaceColl[1], 0);
                WorkPlane wp2 = oComp.WorkPlanes.AddByPlaneAndOffset(oFaceColl[2], 0);

                wpWork = oComp.WorkPlanes.AddByTwoPlanes(wp1, wp2);
                wpWork.Name = "wpWorkReference";
                wpWork.Grounded = true;
                wpWork.Visible = false;

                oBaseFeature.Delete(false, true, true);

                wp1.Delete();
                wp2.Delete();
            }

            return wpWork;
        }
        // ! Aggiunge le linee di taglio
        public static bool addProjectCut(PartDocument oDoc, WorkPlane oWpReference)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;
            
            WorkPlane oWpWork = oCompDef.WorkPlanes.AddByPlaneAndOffset(oWpReference, 0);
            oWpWork.Name = "wpWork";
            oWpWork.Visible = false;

            PlanarSketch oSketch = oCompDef.Sketches.Add(oWpWork);
            ProjectedCut oProjectCut = oSketch.ProjectedCuts.Add();

            int tmpSegmThk = countThicknessSegment(oProjectCut.SketchEntities);

            int loop = 0;
            double offset = 1;
            while (tmpSegmThk != 2)
            {
                // Devo spostare il piano se ci sono cose nel mezzo
                oWpWork.SetByPlaneAndOffset(oWpReference, offset);

                tmpSegmThk = countThicknessSegment(oProjectCut.SketchEntities);
                loop++;

                offset += offset;

                if (loop == 20)
                {
                    throw new Exception("Numero massimo offset piano.");
                }
            }

            oProjectCut.Delete();

            oProjectCut = oSketch.ProjectedCuts.Add();

            List<ObjectCollection> dataLine = splittoLinea(oProjectCut.SketchEntities);

            ObjectCollection linea = lengthPerimetro();

            TransientGeometry oTransGeom = iApp.TransientGeometry;
            UnitVector oNormalVector = oSketch.PlanarEntityGeometry.Normal;
            UnitVector2d oLineDir = linea[1].Geometry.Direction;
            UnitVector oLineVector = (UnitVector)oTransGeom.CreateUnitVector(oLineDir.X, oLineDir.Y, 0);
            UnitVector oOffsetVector = (UnitVector)oLineVector.CrossProduct(oNormalVector);
            UnitVector oDesiredVector = (UnitVector)oTransGeom.CreateUnitVector(0, 1, 0);

            bool bNaturalOffsetDir;

            if (oOffsetVector.IsEqualTo(oDesiredVector))
            {
                bNaturalOffsetDir = true;
            }
            else
            {
                bNaturalOffsetDir = false;
            }


            try
            {
                SketchEntitiesEnumerator oSSketchEntitiesEnum = oSketch.OffsetSketchEntitiesUsingDistance(linea, 0.5, bNaturalOffsetDir, false);
                return true;
            }
            catch
            {
                return false;
            }

            oProjectCut.Delete();

            styleSketch(oSketch.SketchEntities);
            
            int countThicknessSegment(SketchEntitiesEnumerator oSketchEntities)
            {
                int result = 0;

                foreach (SketchEntity e in oSketchEntities)
                {
                    if (e.Type == ObjectTypeEnum.kSketchLineObject)
                    {
                        SketchLine oSketchLine = (SketchLine) e;

                        double length = Math.Round(oSketchLine.Length * 100)/100;

                        if (length == Math.Round(oCompDef.Thickness.Value*100)/100)
                        {
                            result++;
                        }
                    }
                }

                return result;
            }
            List<ObjectCollection> splittoLinea(SketchEntitiesEnumerator oSketchEntities)
            {
                List<ObjectCollection> tmp = new List<ObjectCollection>();
                tmp.Add(iApp.TransientObjects.CreateObjectCollection());
                tmp.Add(iApp.TransientObjects.CreateObjectCollection());
                tmp.Add(iApp.TransientObjects.CreateObjectCollection());

                int indice = 0;
                foreach (SketchEntity oSketchEntity in oSketchEntities)
                {
                    if (oSketchEntity.Type != ObjectTypeEnum.kSketchPointObject)
                    {
                        if (oSketchEntity.Type == ObjectTypeEnum.kSketchLineObject) 
                        {
                            SketchLine oSketchLine = (SketchLine)oSketchEntity;
                            
                            if (Math.Round(oSketchLine.Length * 100) / 100 == Math.Round(oCompDef.Thickness.Value*100)/100)
                            {
                                if(indice == 0)
                                {
                                    indice = 1;
                                }
                                else
                                {
                                    indice = 2;
                                }
                            }
                            else
                            {
                                tmp[indice].Add(oSketchEntity);
                            }
                        }

                        if (oSketchEntity.Type == ObjectTypeEnum.kSketchArcObject) 
                        {
                            tmp[indice].Add(oSketchEntity);
                        }
                    }
                }

                foreach (SketchEntity oSketchEntity in tmp[0])
                {
                    tmp[2].Add(oSketchEntity);
                }

                List<ObjectCollection> result = new List<ObjectCollection>();
                result.Add(tmp[1]);
                result.Add(tmp[2]);

                return result;
            }
            void styleSketch(SketchEntitiesEnumerator oSketchEntities)
            {
                foreach (SketchEntity oSe in oSketchEntities)
                {
                    if (oSe.Type == ObjectTypeEnum.kSketchLineObject)
                    {
                        SketchLine se = (SketchLine)oSe;

                        se.OverrideColor = iApp.TransientObjects.CreateColor(0, 0, 255);
                        se.LineType = LineTypeEnum.kDashDottedLineType;
                    }
                    else if (oSe.Type == ObjectTypeEnum.kSketchArcObject)
                    {
                        SketchArc sa = (SketchArc)oSe;

                        sa.OverrideColor = iApp.TransientObjects.CreateColor(0, 0, 255);
                        sa.LineType = LineTypeEnum.kDashDottedLineType;
                    }
                }
            }
            ObjectCollection lengthPerimetro()
            {
                double l0 = 0;
                double l1 = 0;

                foreach (SketchEntity oSE in dataLine[0])
                {
                    if (oSE.Type == ObjectTypeEnum.kSketchLineObject)
                    {
                        SketchLine se = (SketchLine)oSE;

                        l0 = l0 + se.Length;
                    }
                    else if (oSE.Type == ObjectTypeEnum.kSketchArcObject)
                    {
                        SketchArc se = (SketchArc)oSE;

                        l0 = l0 + se.Length;
                    }
                }
                foreach (SketchEntity oSE in dataLine[1])
                {
                    if (oSE.Type == ObjectTypeEnum.kSketchLineObject)
                    {
                        SketchLine se = (SketchLine)oSE;

                        l1 = l1 + se.Length;
                    }
                    else if (oSE.Type == ObjectTypeEnum.kSketchArcObject)
                    {
                        SketchArc se = (SketchArc)oSE;

                        l1 = l1 + se.Length;
                    }
                }

                if (l0>l1)
                {
                    return dataLine[0];
                }
                else
                {
                    return dataLine[1];
                }
            }
        }
        // ! Setta la texture
        public static void setTexture(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            FaceCollection fcFront = oCompDef.Bends[1].FrontFaces[1].TangentiallyConnectedFaces;
            fcFront.Add(oCompDef.Bends[1].FrontFaces[1]);

            FaceCollection fcBack = oCompDef.Bends[1].BackFaces[1].TangentiallyConnectedFaces;
            fcBack.Add(oCompDef.Bends[1].BackFaces[1]);

            double area0 = 0;
            foreach (Face oFace in fcFront)
            {
                area0 = area0+oFace.Evaluator.Area;
            }

            double area1 = 0;
            foreach (Face oFace in fcBack)
            {
                area1 = area1 + oFace.Evaluator.Area;
            }

            Asset oAsset;

            try
            {
                oAsset = oDoc.Assets["RawSide"];
            }
            catch (System.ArgumentException e)
            {
                Assets oAssets = oDoc.Assets;

                AssetLibrary oAssetsLib = iApp.AssetLibraries["3D_Pisa_Col"];
                Asset oAssetLib = oAssetsLib.AppearanceAssets["RawSide"];

                oAsset = oAssetLib.CopyTo(oDoc);
            }

            FaceCollection fc;

            if (area0>area1)
            {
                fc = fcBack;
            }
            else
            {
                fc = fcFront;
            }

            foreach (Face f in fc)
            {
                f.Appearance = oAsset;
            }
        }
        // ! Prende le facce più grandi della lamiera
        public static Face getBiggestFace(PartDocument oDoc)
        {
            SheetMetalComponentDefinition oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;

            Face biggestFace = null;

            double area = 0;

            foreach (Face f in oCompDef.SurfaceBodies[1].Faces)
            {
                if (f.Evaluator.Area > area)
                {
                    area = f.Evaluator.Area;
                    biggestFace = f;
                }
            }

            return biggestFace;
        }
        // ! Distanza da faccia opposta
        public static double getDistanceFromFace(Face fc)
        {
            Inventor.Point origin = fc.PointOnFace;

            double[] pt = new double[3] { origin.X, origin.Y, origin.Z };

            double[] n = new double[3];

            fc.Evaluator.GetNormalAtPoint(pt, ref n);

            UnitVector normal = iApp.TransientGeometry.CreateUnitVector(-n[0], -n[1], -n[2]);

            SurfaceBody body = fc.Parent;

            ObjectsEnumerator objects;
            ObjectsEnumerator pts;

            body.FindUsingRay(origin, normal, 0.001, out objects, out pts, true);

            double dist = iApp.MeasureTools.GetMinimumDistance(origin, objects[2]) * 10;

            return dist;
        }
        // ! Salva una copia del flat pattern come dxf
        public static bool saveDxf(string pathToSave, PartDocument oDoc)
        {
            //string sOut = "FLAT PATTERN DXF?AcadVersion=2004"
            //    + "&TangentLinesLayer=IV_TANGENT&TangentLinesLayerColor=255;0;0";

            string sOut = "FLAT PATTERN DXF?AcadVersion=2000" 
                + "&OuterProfileLayer=OUTER_PROF&OuterProfileLayerColor=255;0;0" 
                + "&InteriorProfilesLayer=INNER_PROFS&InteriorProfilesLayerColor=0;0;0" 
                + "&FeatureProfileLayer=FEATURE&FeatureProfileLayerColor=0;0;0" 
                + "&BendUpLayer=BEND_UP&BendUpLayerColor=0;255;0&BendUpLayerLineType=37634" 
                + "&BendDownLayer=BEND_DOWN&BendDownLayerColor=0;255;0&BendDownLayerLineType=37634";

            SheetMetalComponentDefinition oCompDef;
            try
            {
                oCompDef = (SheetMetalComponentDefinition)oDoc.ComponentDefinition;
            }
            catch
            {
                return false;
            }

            if (!oCompDef.HasFlatPattern)
            {
                return false;
                throw new Exception("Non è presente il flat pattern!");
            }

            DataIO oDataIO = oCompDef.DataIO;

            string dxfName = @pathToSave+ "\\" + oDoc.DisplayName + ".dxf";

            oDataIO.WriteDataToFile(sOut, dxfName);
            return true;
        }
    }
}