using MongoDB.Driver;
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
        private readonly IMongoCollection<User> _users; // creates the mongo collection of Users
        
        public UserRepository() // constructor for initializing the UserRepository class with the 
        {
            string connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING"); // retreives environment varialbe - mongo conn string
            var client = new MongoClient(connectionString); // creates a new mongo client
            var database = client.GetDatabase("User"); // retreives db
            _users = database.GetCollection<User>("Users"); // retreives collection
        }


        
        //GET
        public virtual async Task<List<User>> GetAllUsers() // method for retreiving allUsers in the collection
        {
            return await _users.Aggregate().ToListAsync();
        }

        public virtual async Task<User> GetUserById(int userId) // method for retreiving a specific User in the collection
        {
            var filter = Builders<User>.Filter.Eq("UserId", userId);
            Console.WriteLine("repository - GetUserById");
            Console.WriteLine("id: " + userId);
            Console.WriteLine("database: " + _users);

            return await _users.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<int?> GetNextUserId() // method for retreiving the highest+1 userId in the collection
        {
            var lastUser = _users.AsQueryable().OrderByDescending(a => a.UserId).FirstOrDefault(); // retreives allUsers and orders them by userId in descending order
            return (lastUser != null) ? lastUser.UserId + 1 : 1; // adds 1 to the current highest userId
        }


        

        

        //POST
        public virtual async Task AddNewUser(User? user) // method for adding a new User to the collection
        {
            await Task.Run(() => _users.InsertOne(user!));
        }


        



        //PUT
        public virtual async Task UpdateUser(int userId, User user) // method for updating specified User attributes
        {
            var filter = Builders<User>.Filter.Eq(a => a.UserId, userId);
            var update = Builders<User>.Update.
                // updates desired attributes
                Set(a => a.UserName, user.UserName).
                Set(a => a.UserPassword, user.UserPassword).
                Set(a => a.UserEmail, user.UserEmail).
                Set(a => a.UserPhone, user.UserPhone);
            
            await _users.UpdateOneAsync(filter, update);
        }






        //DELETE
        public virtual async Task DeleteUser(int userId) // method for deleting a User from the collection
        {
            var filter = Builders<User>.Filter.Eq(a => a.UserId, userId); // retreives the specified userId
            await _users.DeleteOneAsync(filter);
        }
    }
}
