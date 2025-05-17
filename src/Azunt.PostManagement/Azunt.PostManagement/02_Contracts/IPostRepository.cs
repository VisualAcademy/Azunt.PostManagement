﻿using Azunt.Models.Common;

namespace Azunt.PostManagement;

/// <summary>
/// Post 전용 확장 저장소 인터페이스 - 페이징, 검색 기능 포함
/// </summary>
public interface IPostRepository : IPostRepositoryBase
{
    /// <summary>
    /// 페이징 + 검색 기능 제공
    /// </summary>
    Task<ArticleSet<Post, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier, string category = "");

    /// <summary>
    /// 필터 옵션 기반 조회 기능 제공
    /// </summary>
    Task<ArticleSet<Post, long>> GetAllAsync<TParentIdentifier>(
        Dul.Articles.FilterOptions<TParentIdentifier> options);

    Task<bool> MoveUpAsync(long id);
    Task<bool> MoveDownAsync(long id);
}