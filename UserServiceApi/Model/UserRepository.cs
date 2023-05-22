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
        private readonly IMongoCollection<User> _user;

        public UserRepository()
        {
            string connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING"); // mongo conn string miljøvariabel
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("User"); // vores database
            _user = database.GetCollection<User>("Users");
            Console.WriteLine("mongo conn string: " + connectionString);
        }
        
        
        public async Task<User> FindUserByUsernameAndPassword(string userName, string userPassword)
        {
            var filter = Builders<User>.Filter.Eq("UserName", userName) & Builders<User>.Filter.Eq("UserPassword", userPassword);
            return await _user.Find(filter).FirstOrDefaultAsync();
        }





        //GET
        public async Task<User> GetUserById(int id)
        {
            var filter = Builders<User>.Filter.Eq("UserId", id);
            Console.WriteLine("repository - GetUserById");
            Console.WriteLine("id: " + id);
            Console.WriteLine("database: " + _user);

            return await _user.Find(filter).FirstOrDefaultAsync();
        }

        public int GetNextUserId()
        {
            var lastUser = _user.AsQueryable().OrderByDescending(a => a.UserId).FirstOrDefault();
            return (lastUser != null) ? lastUser.UserId + 1 : 1;
        }




        

        //POST
        public void AddNewUser(User? user)
        {
            _user.InsertOne(user!);
        }


        



        //PUT
        public async Task UpdateUser(int userId, User user)
        {
            var filter = Builders<User>.Filter.Eq(a => a.UserId, userId);
            var update = Builders<User>.Update.
                Set(a => a.UserName, user.UserName).
                Set(a => a.UserPassword, user.UserPassword).
                Set(a => a.UserEmail, user.UserEmail).
                Set(a => a.UserPhone, user.UserPhone);
            
            await _user.UpdateOneAsync(filter, update);
        }






        //DELETE
        public async Task DeleteUser(int userId)
        {
            var filter = Builders<User>.Filter.Eq(a => a.UserId, userId);
            await _user.DeleteOneAsync(filter);
        }
    }
}
