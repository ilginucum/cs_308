using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using e_commerce.Models;

namespace e_commerce.Data
{
    public interface IMongoDBRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<T> FindOneAsync(Expression<Func<T, bool>> filterExpression);
        Task<IEnumerable<T>> FilterByAsync(Expression<Func<T, bool>> filterExpression);
        Task InsertOneAsync(T document);
        Task UpdateOneAsync(string id, T document);
        Task DeleteOneAsync(string id);
    }

    public class MongoDBRepository<T> : IMongoDBRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDBRepository(IConfiguration configuration, string collectionName)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
            var database = client.GetDatabase(configuration["Database:Name"]);
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<T> FindOneAsync(Expression<Func<T, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FilterByAsync(Expression<Func<T, bool>> filterExpression)
        {
            return await _collection.Find(filterExpression).ToListAsync();
        }

        public async Task InsertOneAsync(T document)
        {
            await _collection.InsertOneAsync(document);
        }

        public async Task UpdateOneAsync(string id, T document)
        {
            await _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("Id", id), document);
        }

        public async Task DeleteOneAsync(string id)
        {
            await _collection.DeleteOneAsync(Builders<T>.Filter.Eq("Id", id));
        }
    }
}