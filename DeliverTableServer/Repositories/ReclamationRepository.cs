<<<<<<< HEAD
using DeliverTableServer.Data;
=======
﻿using DeliverTableServer.Data;
>>>>>>> 5902b14 (feat(client/server): Implement Admin + Restaurant Reclamation management)
using DeliverTableServer.Mappers;
using DeliverTableServer.Models;
using DeliverTableServer.Repositories.Interfaces;
using DeliverTableSharedLibrary.Dtos.Reclamation;
using DeliverTableSharedLibrary.Enums;
using Microsoft.EntityFrameworkCore;

namespace DeliverTableServer.Repositories;

public class ReclamationRepository(DeliverTableContext dbContext) : IReclamationRepository
{
<<<<<<< HEAD
<<<<<<< HEAD
    public async Task<List<ReclamationDto>> GetAllReclamations(ReclamationQuery query)
    {
        IQueryable<Reclamation> queryable = dbContext.Reclamations.AsQueryable();
        queryable = ApplyQueryFilters(queryable, query);
        queryable = queryable.Include(r => r.Items).ThenInclude(i => i.OrderItem);
        return await queryable.Select(r => r.ToDto()).ToListAsync();
=======
    public async Task<List<Reclamation>> GetAllReclamations(ReclamationQuery query)
=======
    public async Task<List<ReclamationDto>> GetAllReclamations(ReclamationQuery query)
>>>>>>> 5902b14 (feat(client/server): Implement Admin + Restaurant Reclamation management)
    {
        IQueryable<Reclamation> queryable = dbContext.Reclamations.AsQueryable();
        queryable = ApplyQueryFilters(queryable, query);
        queryable = queryable.Include(r => r.Items).ThenInclude(i => i.OrderItem);
        return await queryable.Select(r => r.ToDto()).ToListAsync();
    }

    public async Task<Reclamation?> GetReclamationById(int reclamationId)
    {
        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Include(r => r.Order)
            .ThenInclude(o => o.Restaurant)
            .FirstOrDefaultAsync(r => r.ReclamationId == reclamationId);
    }

    public async Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation)
    {
        var entity = new Reclamation
        {
            OrderId = reclamation.OrderId,
            Description = reclamation.Description,
            Status = ReclamationStatus.Pending,
            Type = Enum.Parse<ReclamationType>(reclamation.Type),
            Items = [..reclamation.Items.Select(i => new ReclamationItem
            {
                OrderItemId = i.OrderItemId,
                HasAttachedImage = i.HasImage
            })]
        };

        dbContext.Reclamations.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(entity.ReclamationId) ?? new Reclamation();
    }

    public async Task<Reclamation?> GetReclamationsByOrderId(int orderId)
    {
        return await dbContext.Reclamations
                .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    public async Task<List<Reclamation>> GetReclamationsByUser(int userId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.CustomerId == userId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Where(r => orderIds.Contains(r.OrderId))
            .ToListAsync();
    }

    public async Task<List<Reclamation>> GetReclamationsByRestaurant(int restaurantId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.RestaurantId == restaurantId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Where(r => orderIds.Contains(r.OrderId))
            .ToListAsync();
    }

    public async Task<List<Reclamation>> GetReclamationsByRestaurantOwner(int ownerId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.Restaurant.OwnerId == ownerId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Include(r => r.Order)
            .ThenInclude(o => o.Restaurant)
            .Where(r => orderIds.Contains(r.OrderId))
            .OrderByDescending(r => r.Created)
            .ToListAsync();
    }

    public async Task<Reclamation?> UpdateReclamationStatus(int reclamationId, ReclamationStatus status)
    {
        Reclamation? existing = await dbContext.Reclamations.FindAsync(reclamationId);
        if (existing == null) return null;

        existing.Status = status;
        existing.Updated = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(reclamationId);
    }

    public async Task<Reclamation?> UpdateReclamation(int reclamationId, UpdateReclamationDto reclamation)
    {
        Reclamation? existingReclamation = await dbContext.Reclamations.FindAsync(reclamationId);
        if (existingReclamation == null) return null;

        existingReclamation.Type = reclamation.Type;
        existingReclamation.Status = reclamation.Status;
        existingReclamation.Description = reclamation.Description;
        existingReclamation.RefundAmount = reclamation.RefundAmount;
        existingReclamation.Updated = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(reclamationId);
    }

    public async Task<bool> DeleteReclamation(int reclamationId)
    {
        Reclamation? reclamation = await dbContext.Reclamations.FindAsync(reclamationId);
        if (reclamation == null) return false;

        dbContext.Reclamations.Remove(reclamation);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static IQueryable<Reclamation> ApplyQueryFilters(
        IQueryable<Reclamation> queryable,
        ReclamationQuery query)
    {
        if (!string.IsNullOrEmpty(query.ReclamationType) &&
            Enum.TryParse<ReclamationType>(query.ReclamationType, ignoreCase: true, out var type))
        {
            queryable = queryable.Where(r => r.Type == type);
        }

        if (!string.IsNullOrEmpty(query.ReclamationStatus) &&
            Enum.TryParse<ReclamationStatus>(query.ReclamationStatus, ignoreCase: true, out var status))
        {
            queryable = queryable.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(query.Content))
        {
            queryable = queryable.Where(r => r.Description.Contains(query.Content));
        }

        queryable = queryable
            .Skip((int)((query.PageNumber - 1) * query.PageSize))
            .Take((int)query.PageSize);

        return queryable;
>>>>>>> 8e99819 (feat(client/server): Adding Reclamation creation)
    }
<<<<<<< HEAD

    public async Task<Reclamation?> GetReclamationById(int reclamationId)
    {
        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Include(r => r.Order)
            .ThenInclude(o => o.Restaurant)
            .FirstOrDefaultAsync(r => r.ReclamationId == reclamationId);
    }

    public async Task<Reclamation> CreateReclamation(CreateReclamationDto reclamation)
    {
        var entity = new Reclamation
        {
            OrderId = reclamation.OrderId,
            Description = reclamation.Description,
            Status = ReclamationStatus.Pending,
            Type = Enum.Parse<ReclamationType>(reclamation.Type),
            Items = [..reclamation.Items.Select(i => new ReclamationItem
            {
                OrderItemId = i.OrderItemId,
                HasAttachedImage = i.HasImage
            })]
        };

        dbContext.Reclamations.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(entity.ReclamationId) ?? new Reclamation();
    }

    public async Task<Reclamation?> GetReclamationsByOrderId(int orderId)
    {
        return await dbContext.Reclamations
                .Include(r => r.Items)
                .ThenInclude(i => i.OrderItem)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
    }

    public async Task<List<Reclamation>> GetReclamationsByUser(int userId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.CustomerId == userId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Where(r => orderIds.Contains(r.OrderId))
            .ToListAsync();
    }

    public async Task<List<Reclamation>> GetReclamationsByRestaurant(int restaurantId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.RestaurantId == restaurantId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Where(r => orderIds.Contains(r.OrderId))
            .ToListAsync();
    }

    public async Task<List<Reclamation>> GetReclamationsByRestaurantOwner(int ownerId)
    {
        var orderIds = await dbContext.Orders
            .Where(o => o.Restaurant.OwnerId == ownerId)
            .Select(o => o.Id)
            .ToListAsync();

        return await dbContext.Reclamations
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Include(r => r.Order)
            .ThenInclude(o => o.Restaurant)
            .Where(r => orderIds.Contains(r.OrderId))
            .OrderByDescending(r => r.Created)
            .ToListAsync();
    }

    public async Task<Reclamation?> UpdateReclamationStatus(int reclamationId, ReclamationStatus status)
    {
        Reclamation? existing = await dbContext.Reclamations.FindAsync(reclamationId);
        if (existing == null) return null;

        existing.Status = status;
        existing.Updated = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(reclamationId);
    }

    public async Task<Reclamation?> UpdateReclamation(int reclamationId, UpdateReclamationDto reclamation)
    {
        Reclamation? existingReclamation = await dbContext.Reclamations.FindAsync(reclamationId);
        if (existingReclamation == null) return null;

        existingReclamation.Type = reclamation.Type;
        existingReclamation.Status = reclamation.Status;
        existingReclamation.Description = reclamation.Description;
        existingReclamation.RefundAmount = reclamation.RefundAmount;
        existingReclamation.Updated = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetReclamationById(reclamationId);
    }

    public async Task<bool> DeleteReclamation(int reclamationId)
    {
        Reclamation? reclamation = await dbContext.Reclamations.FindAsync(reclamationId);
        if (reclamation == null) return false;

        dbContext.Reclamations.Remove(reclamation);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private static IQueryable<Reclamation> ApplyQueryFilters(
        IQueryable<Reclamation> queryable,
        ReclamationQuery query)
    {
        if (!string.IsNullOrEmpty(query.ReclamationType) &&
            Enum.TryParse<ReclamationType>(query.ReclamationType, ignoreCase: true, out var type))
        {
            queryable = queryable.Where(r => r.Type == type);
        }

        if (!string.IsNullOrEmpty(query.ReclamationStatus) &&
            Enum.TryParse<ReclamationStatus>(query.ReclamationStatus, ignoreCase: true, out var status))
        {
            queryable = queryable.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(query.Content))
        {
            queryable = queryable.Where(r => r.Description.Contains(query.Content));
        }

        queryable = queryable
            .Skip((int)((query.PageNumber - 1) * query.PageSize))
            .Take((int)query.PageSize);

        return queryable;
    }
=======
>>>>>>> 5902b14 (feat(client/server): Implement Admin + Restaurant Reclamation management)
}
