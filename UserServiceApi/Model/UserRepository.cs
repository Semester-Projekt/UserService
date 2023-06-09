﻿using MongoDB.Driver;
using System.Threading.Tasks;
using Model;
using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Model
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _users;
        
        public UserRepository()
        {
            string connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING")!; // retreives environment variable from the Service Deployment file
            var client = new MongoClient(connectionString); // creates a new mongo client
            var database = client.GetDatabase("User"); // retreives db
            _users = database.GetCollection<User>("Users"); // retreives collection
        }

        
        //GET
        public virtual async Task<List<User>> GetAllUsers() // method for retreiving allUsers in the collection
        {
            return await _users.Aggregate().ToListAsync();
        }

        public virtual async Task<User> GetUserByUserName(string userName) // method for retreiving a specific User in the collection
        {
            var filter = Builders<User>.Filter.Eq("UserName", userName);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }


        

        

        //POST
        public virtual async Task AddNewUser(User? user) // method for adding a new User to the collection
        {
            await Task.Run(() => _users.InsertOne(user!));
        }


        



        //PUT
        public virtual async Task UpdateUser(string? userName, User user) // method for updating specified User attributes
        {
            // filters on specified userName. .Set updates the specified attributes and replaces them with the updated data
            var filter = Builders<User>.Filter.Eq(a => a.UserName, userName);
            var update = Builders<User>.Update.
                Set(a => a.UserName, user.UserName).
                Set(a => a.UserPassword, user.UserPassword).
                Set(a => a.UserEmail, user.UserEmail).
                Set(a => a.UserPhone, user.UserPhone);
            
            await _users.UpdateOneAsync(filter, update);
        }






        //DELETE
        public virtual async Task DeleteUser(int? userId) // method for deleting a User from the collection
        {
            var filter = Builders<User>.Filter.Eq(a => a.UserId, userId); // retreives the specified userId
            await _users.DeleteOneAsync(filter);
        }
    }
}