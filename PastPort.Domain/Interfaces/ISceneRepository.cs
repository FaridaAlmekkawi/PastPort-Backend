using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PastPort.Domain.Entities;

namespace PastPort.Domain.Interfaces;

public interface ISceneRepository : IRepository<HistoricalScene>
{
    Task<IEnumerable<HistoricalScene>> GetByEraAsync(string era);
    Task<IEnumerable<HistoricalScene>> SearchScenesAsync(string searchTerm);
    Task<HistoricalScene?> GetSceneWithCharactersAsync(Guid sceneId);
}