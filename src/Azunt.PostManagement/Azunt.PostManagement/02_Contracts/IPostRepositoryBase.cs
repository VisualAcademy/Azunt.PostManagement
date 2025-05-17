using Azunt.Repositories;

namespace Azunt.PostManagement;

/// <summary>
/// 기본 CRUD 작업을 위한 Post 전용 저장소 인터페이스
/// </summary>
public interface IPostRepositoryBase : IRepositoryBase<Post, long>
{
}
