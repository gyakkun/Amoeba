using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Amoeba.Interface.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string name)
        {
            if (name == "Exit")
            {
                Program.Exit();
            }

            var tempList = new List<SearchListViewItemInfo>();

            foreach (var info in Amoeba.Message.GetSearchListViewItemInfos())
            {
                if (!info.Name.Contains(name)) continue;
                tempList.Add(info);
            }

            return View(tempList.ToArray());
        }
    }
}
