using Azunt.Models.Common;
using Azunt.PostManagement;
using Azunt.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azunt.PostManagement;

/// <summary>
/// Post 테이블에 대한 Entity Framework Core 기반 리포지토리 구현체입니다.
/// </summary>
public class PostRepository : IPostRepository
{
    private readonly PostDbContextFactory _factory;
    private readonly ILogger<PostRepository> _logger;
    private readonly string? _connectionString;

    public PostRepository(
        PostDbContextFactory factory,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<PostRepository>();
    }

    public PostRepository(
        PostDbContextFactory factory,
        ILoggerFactory loggerFactory,
        string connectionString)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<PostRepository>();
        _connectionString = connectionString;
    }

    private PostDbContext CreateContext() =>
        string.IsNullOrWhiteSpace(_connectionString)
            ? _factory.CreateDbContext()
            : _factory.CreateDbContext(_connectionString);

    public async Task<Post> AddAsyncDefault(Post model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;
        context.Posts.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<Post> AddAsync(Post model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;

        // 현재 가장 높은 DisplayOrder 값 조회
        var maxDisplayOrder = await context.Posts
            .Where(m => !m.IsDeleted)
            .MaxAsync(m => (int?)m.DisplayOrder) ?? 0;

        model.DisplayOrder = maxDisplayOrder + 1;

        context.Posts.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<IEnumerable<Post>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.Posts
            .Where(m => !m.IsDeleted)
            //.OrderByDescending(m => m.Id)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Post> GetByIdAsync(long id)
    {
        await using var context = CreateContext();
        return await context.Posts
            .Where(m => m.Id == id && !m.IsDeleted)
            .SingleOrDefaultAsync()
            ?? new Post();
    }

    public async Task<bool> UpdateAsync(Post model)
    {
        await using var context = CreateContext();
        context.Attach(model);
        context.Entry(model).State = EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var context = CreateContext();
        var entity = await context.Posts.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsDeleted = true;
        context.Posts.Update(entity);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<Azunt.Models.Common.ArticleSet<Post, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier,
        string category = "")
    {
        await using var context = CreateContext();
        var query = context.Posts
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        // category 필터 적용
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(m => m.Category == category);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Name != null && m.Name.Contains(searchQuery));
        }

        query = sortOrder switch
        {
            "Name" => query.OrderBy(m => m.Name),
            "NameDesc" => query.OrderByDescending(m => m.Name),
            "DisplayOrder" => query.OrderBy(m => m.DisplayOrder),
            _ => query.OrderBy(m => m.DisplayOrder)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Azunt.Models.Common.ArticleSet<Post, int>(items, totalCount);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.Posts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var upper = await context.Posts
            .Where(x => x.DisplayOrder < current.DisplayOrder && !x.IsDeleted)
            .OrderByDescending(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (upper == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = upper.DisplayOrder;
        upper.DisplayOrder = temp;

        // 명시적 변경 추적
        context.Posts.Update(current);
        context.Posts.Update(upper);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.Posts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var lower = await context.Posts
            .Where(x => x.DisplayOrder > current.DisplayOrder && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (lower == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = lower.DisplayOrder;
        lower.DisplayOrder = temp;

        // 명시적 변경 추적
        context.Posts.Update(current);
        context.Posts.Update(lower);

        await context.SaveChangesAsync();
        return true;
    }
}
