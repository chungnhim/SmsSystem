using Microsoft.EntityFrameworkCore;
using Sms.Web.Entity;
using Sms.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sms.Web.Service
{
    public interface IServiceBase<T> where T : BaseEntity
    {
        Task<List<T>> GetAlls();
        Task<T> Get(int id);
        Task<ApiResponseBaseModel<T>> Update(T model);
        Task<ApiResponseBaseModel<T>> Create(T model);
        Task<ApiResponseBaseModel<int>> Delete(int id);
        Task<FilterResponse<T>> Paging(FilterRequest filterRequest);
    }

    public abstract class ServiceBase<T> : IServiceBase<T> where T : BaseEntity
    {
        protected readonly SmsDataContext _smsDataContext;
        public ServiceBase(SmsDataContext smsDataContext)
        {
            this._smsDataContext = smsDataContext;
        }

        public virtual async Task<ApiResponseBaseModel<T>> Create(T model)
        {
            model.Id = 0;
            var validateResult = await ValidateEntry(model);
            if (!string.IsNullOrEmpty(validateResult))
            {
                return new ApiResponseBaseModel<T>()
                {
                    Success = false,
                    Message = validateResult
                };
            }
            _smsDataContext.Set<T>().Add(model);
            await _smsDataContext.SaveChangesAsync();
            await AfterCreated();
            return new ApiResponseBaseModel<T>()
            {
                Success = true,
                Results = model
            };
        }

        public virtual async Task<ApiResponseBaseModel<int>> Delete(int id)
        {
            var entity = await Get(id);
            if (entity == null)
            {
                return new ApiResponseBaseModel<int>()
                {
                    Message = "NotFound",
                    Results = 0,
                    Success = false
                };
            }
            _smsDataContext.Set<T>().Remove(entity);
            await AfterDeleted();
            return new ApiResponseBaseModel<int>()
            {
                Results = await _smsDataContext.SaveChangesAsync(),
                Success = true
            };
        }

        public virtual async Task<T> Get(int id)
        {
            return await GenerateQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        public virtual async Task<List<T>> GetAlls()
        {
            return await GenerateQuery().ToListAsync();
        }

        public virtual async Task<ApiResponseBaseModel<T>> Update(T model)
        {
            var entity = await Get(model.Id);
            if (entity == null)
            {
                return new ApiResponseBaseModel<T>()
                {
                    Success = false,
                    Message = "NotFound"
                };
            }
            Map(entity, model);
            var validateResult = await ValidateEntry(entity);
            if (!string.IsNullOrEmpty(validateResult))
            {
                return new ApiResponseBaseModel<T>()
                {
                    Success = false,
                    Message = validateResult,
                };
            }
            await _smsDataContext.SaveChangesAsync();
            await AfterUpdated();
            return new ApiResponseBaseModel<T>() { Results = entity, Success = true };
        }

        public abstract void Map(T entity, T model);

        public virtual async Task<FilterResponse<T>> Paging(FilterRequest filterRequest)
        {
            var pageSize = Math.Min(1000, filterRequest.PageSize);
            if (pageSize == 0) pageSize = 20;
            var query = GenerateQuery(filterRequest);
            var count = await query.CountAsync();
            var list = await query.Skip(filterRequest.PageIndex * pageSize).Take(pageSize).ToListAsync();
            var additionalData = new Dictionary<string, object>();
            var ignoreAdditionalData = false;
            if (filterRequest != null && filterRequest.SearchObject != null)
            {
                if (filterRequest.SearchObject.ContainsKey("IgnoreAdditionalData"))
                {
                    ignoreAdditionalData = true;
                }
            }
            if (!ignoreAdditionalData)
            {
                additionalData = await GetFilterAdditionalData(filterRequest, query);
            }

            return new FilterResponse<T>()
            {
                Total = count,
                Results = await PagingResultsMap(list),
                AdditionalData = additionalData
            };
        }

        protected virtual async Task<List<T>> PagingResultsMap(List<T> entities)
        {
            return entities;
        }

        public virtual Task<Dictionary<string,object>> GetFilterAdditionalData(FilterRequest filterRequest, IQueryable<T> query)
        {
            return Task.FromResult(new Dictionary<string, object>());
        }
        protected virtual async Task<string> ValidateEntry(T entity)
        {
            return await Task.Run<string>(() => string.Empty);
        }
        protected virtual IQueryable<T> GenerateQuery(FilterRequest filterRequest = null)
        {
            return _smsDataContext.Set<T>().OrderByDescending(r => r.Created);
        }

        protected async Task UpdateUserBallance(int userId, decimal amount)
        {
            var sql = @"UPDATE Users set Ballance = Ballance + {0} where Id = {1} and Ballance + {0} >= 0";
            var affectedCount = await _smsDataContext.Database.ExecuteSqlCommandAsync(
                sql,
                amount,
                userId
                );
            if(affectedCount < 1) throw new ArgumentException("Balance too low!");
            var updateTransactionSql = @"
Update UserTransactions
set UserTransactions.Balance = Users.Ballance
from UserTransactions
join Users on UserTransactions.UserId = Users.Id
where UserTransactions.Id in (select top 1 id from UserTransactions where UserId = {0} order by id desc)";

            await _smsDataContext.Database.ExecuteSqlCommandAsync(
                updateTransactionSql,
                userId);
        }
        protected async virtual Task AfterCreated()
        {
        }

        protected async virtual Task AfterUpdated()
        {

        }

        protected async virtual Task AfterDeleted()
        {

        }

    }
}
