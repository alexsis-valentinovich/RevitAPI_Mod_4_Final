using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Document = Autodesk.Revit.DB.Document;

namespace RevitAPI_Mod_4_Final
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            List<ViewPlan> views = new List<ViewPlan>(new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where<ViewPlan>(view => ViewType.FloorPlan == view.ViewType));
            for(int i=0; i<=views.Count-1; i++)
            {
                bool Flag = views[i].Title.Contains("План этаж");
                if(Flag == false)
                {
                    views.RemoveAt(i);
                    i--;
                }
            }

            List<Element> rooms = new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement))
                .WhereElementIsNotElementType()
                .Where(room => room.GetType() == typeof(Room))
                 .ToList();

            ViewPlan viewPlanTemp;
            for (int i = 0; i < views.Count - 1; i++)
            {
                for (int j = i + 1; j < views.Count; j++)
                {
                    if (views[i].GenLevel.Elevation > views[j].GenLevel.Elevation)
                    {
                        viewPlanTemp = views[i];
                        views[i] = views[j];
                        views[j] = viewPlanTemp;
                    }
                }
            }

            var roomTags = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_RoomTags)
                .Cast<FamilySymbol>()
            .FirstOrDefault();

            Transaction ts = new Transaction(doc);
            int nomerFloor = 1;
            ts.Start("Установка марок помещений");
            foreach (View vP in views)
            {
                int nomerRoom = 1;
                View v = vP;
                foreach (Room r in rooms)
                {
                    if (vP.GenLevel.Id == r.Level.Id)
                    {
                        LocationPoint locPoint = r.Location as LocationPoint;//first var
                        UV center = new UV(locPoint.Point.X, locPoint.Point.Y);
                        //XYZ roomCenter = GetElementCenter(r);//secong var
                        // UV center = new UV(roomCenter.X, roomCenter.Y);
                        doc.Create.NewRoomTag(new LinkElementId(r.Id), center, v.Id);
                        r.Number = Convert.ToString(nomerFloor) + "." + Convert.ToString(nomerRoom);
                        nomerRoom++;
                    }
                }
                nomerFloor++;
            }
            ts.Commit();

            return Result.Succeeded;
        }

        private XYZ GetElementCenter(Room r)
        {
            BoundingBoxXYZ bounding = r.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }
    }
}
