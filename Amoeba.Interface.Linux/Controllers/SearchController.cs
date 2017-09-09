using System.Collections.Generic;
using Amoeba.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Amoeba.Interface.Controllers
{
    public class SearchController : Controller
    {
        public IActionResult Index(string name)
        {
            var seeds = new List<Seed>();
            seeds.Add(AmoebaConverter.FromSeedString("Seed:AAAAEEFtb2ViYSA1LjAuMC56aXABgpiCHgIUMjAxNy0wNi0yNVQwODo0MToyMloDJgABASIAIFSvs1EzXh-JlEBDTpAARAfg4yoS736kmsGo8Pbc1JLmGuj6Tw"));
            seeds.Add(AmoebaConverter.FromSeedString("Seed:AAAAEEFtb2ViYSA1LjAuMC56aXABgpiCHgIUMjAxNy0wNi0yNVQwODo0MToyMloDJgABASIAIFSvs1EzXh-JlEBDTpAARAfg4yoS736kmsGo8Pbc1JLmGuj6Tw"));
            seeds.Add(AmoebaConverter.FromSeedString("Seed:AAAAEEFtb2ViYSA1LjAuMC56aXABgpiCHgIUMjAxNy0wNi0yNVQwODo0MToyMloDJgABASIAIFSvs1EzXh-JlEBDTpAARAfg4yoS736kmsGo8Pbc1JLmGuj6Tw"));
            seeds.Add(AmoebaConverter.FromSeedString("Seed:AAAAEEFtb2ViYSA1LjAuMC56aXABgpiCHgIUMjAxNy0wNi0yNVQwODo0MToyMloDJgABASIAIFSvs1EzXh-JlEBDTpAARAfg4yoS736kmsGo8Pbc1JLmGuj6Tw"));

            return View(seeds);
        }
    }
}
