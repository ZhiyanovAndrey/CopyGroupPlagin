using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            Reference reference = uidoc.Selection.PickObject(ObjectType.Element, "Выберете группу элементов"); 
            //результат ССЫЛКА                                                                                                           
            //если хотим изменить обьект то ссылки не достаточно преобразуем ее в элемент
            Element element = doc.GetElement(reference);
            Group group = element as Group; //предпочтительный метод преобр., т.к. если преобразование не случится будет Null

            XYZ point = uidoc.Selection.PickPoint("Выберете точку");

            Transaction transaction = new Transaction(doc);
            transaction.Start("Копирование группы обьектов");
            //обращаемся к базе данных модели к его свойству Create и далее к методу
            doc.Create.PlaceGroup(point, group.GroupType);
            transaction.Commit();

            return Result.Succeeded;
        }
    }
}
