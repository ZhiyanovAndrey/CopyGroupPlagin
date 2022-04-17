using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlagin
{
    [TransactionAttribute(TransactionMode.Manual)]
    //будем управлять сами из кода в какой момент начать и завершить транзакцию
    public class CopyGroup : IExternalCommand

    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                //создадим экземпляр класса и передадим его в метод PickObject
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберете группу элементов");
                //результат ССЫЛКА                                                                                                           
                //если хотим изменить обьект то ссылки не достаточно преобразуем ее в элемент
                Element element = doc.GetElement(reference);
                Group group = element as Group; //предпочтительный метод преобр., т.к. если преобразование не случится будет Null
                XYZ groupCenter = GetElementCenter(group); //находим центр рамки
                Room room = GetRoomByPoint(doc, groupCenter); //комната в которой группа
                XYZ roomCenter = GetElementCenter(room);//найдем ее центр
                XYZ offset = groupCenter - roomCenter; //найдем смещение между центрами

                XYZ point = uidoc.Selection.PickPoint("Выберете точку");

                Room desiredRoom = GetRoomByPoint(doc, point); //найдем комнату с точкой поставил пользователь
                XYZ desiredroomCenter = GetElementCenter(desiredRoom);//найдем центр этой комнаты
                XYZ desiredGroupCenter = desiredroomCenter+offset; //вычислим смещение для вставки


                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы обьектов");
                //обращаемся к базе данных модели к его свойству Create и далее к методу
                doc.Create.PlaceGroup(desiredGroupCenter, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) //при нажатии Esc
            {
                return Result.Cancelled;
            }
            catch (Exception ex) //при прочих ошибках
            {
                message = ex.Message; //передадим текст ошибки
                return Result.Failed;
            }
            return Result.Succeeded;
        }


        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            //центр это среднее ариф. от мин и макс точки рамки
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point)) //если комната содержит точку то возвращаем ее в качестве ответа
                    {
                        return room;
                    }
                }
            }
            return null; //в остальных случаях null
        }


    }
    public class GroupPickFilter : ISelectionFilter //класс необходим для подсветки только группы
    {
        public bool AllowElement(Element elem) //если эл. явл. группой то true
        {
            //приводим обе стороны к int
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else return false;



        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
