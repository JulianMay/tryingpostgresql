﻿using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostgresLol
{
    public static class Connection {
        public static IDbConnection Make()
        {
            var c = new Npgsql.NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=Test;User Id=postgres;Password=Q123w234; Enlist=true");
   
            return c;
        }
    }


    public class RegistrationReadService
    {
        public IEnumerable<Registration> SearchRegistrations(RegistrationQuery query)
        {
            using (var d = Connection.Make())
            {
                var qry = d.QueryMultiple("select name, id from users; select entity from registrations;");
                var users = new User.Cache(qry.Read().ToDictionary(u => (Guid)u.id, u => (string)u.name));
                foreach (var e in qry.Read())
                {
                    var r = ((string)e.entity).Deserialized<Registration>();
                    r.Responsible = users[r.ResponsibleId];
                    yield return r;
                }
            }
        }

        public Tuple<string, DateTime>[] GetRegistrationLog(Guid registrationId)
        {
            throw new NotImplementedException();
        }
    }

    public class RegistrationEventHandler
    {
        private readonly Dictionary<Guid, string> _userCache;
        private readonly User.Cache UserCache;
        public RegistrationEventHandler(Dictionary<Guid, string> userCache = null)
        {
            _userCache = userCache ?? new Dictionary<Guid, string>();
            UserCache = new User.Cache(_userCache);
        }
            

        public void RegistrationRegistered(Guid registrationId,string description, Guid responsibleUserId)
        {
            var r = Registration.MakeBlank(registrationId);
            r.Description = description;
            r.ResponsibleId = responsibleUserId;
            r.Created = DateTime.Now;
            r.LatestChange = DateTime.Now;
            r.State = RegistrationState.Open;

            using (var d = Connection.Make())
            {
                d.Execute($"INSERT INTO registrations (id,entity) VALUES ('{r.Id}', '{r.Serialized()}')");
            }
        }

        public void UserCreated(string name, Guid id)
        {
            using (var d = Connection.Make())
            {
                d.Execute($"INSERT INTO users (id,name) VALUES ('{id}', '{name}')");
            }
        }

        public void CommentAddedToRegistration(Guid registrationId, string text, Guid commentorUserId, DateTime timestamp)
        {
            throw new NotImplementedException();
        }

        public void UserAssignedRegistration(Guid registrationId, Guid userId)
        {
            using (var d = Connection.Make())
            {
                var r = GetRegistration(registrationId,d);
                r.Assignees = r.Assignees.With(UserCache[userId]);
                Update(r,d);
            }
        }

        public void RegistrationStateChanged(Guid registrationId, RegistrationState newState)
        {
            throw new NotImplementedException();
        }

        private Registration GetRegistration(Guid id, IDbConnection d)
        {
                var entity = d.Query<string>($"select entity from registrations where Id = '{id}'").FirstOrDefault();
                if (string.IsNullOrEmpty(entity))
                    return Registration.MakeBlank(id);
                return (entity).Deserialized<Registration>();
        }

        private void Update(Registration r, IDbConnection d)
        {
            d.Execute($"update registrations set entity = '{r.Serialized()}' where id = '{r.Id}'");
        }
    }

    


    public struct RegistrationQuery
    {
        internal Guid ForUser;
    }

    public class Registration
    {
        public Guid Id;
        public string Description;
        public RegistrationState State;
        public DateTime Created;
        public DateTime LatestChange;
        public Guid ResponsibleId;
        public User Responsible;
        public User[] Assignees;
        public RegistrationComment[] Comments;


        private Registration() { /* Use factory methods plz */}

        public static Registration MakeBlank(Guid id)
        {
            return new Registration
            {
                Id = id,
                Description = string.Empty,
                Assignees = new User[0],
                Comments = new RegistrationComment[0]
            };
        }


    }

    public enum RegistrationState { Open, ReadyForInspection, NeedsMoreWork, Accepted, Declined }

    public class RegistrationComment
    {
        public readonly string Text;
        public readonly DateTime Timestamp;
        public readonly User Commentor;
    }

    public class User
    {
        public string Name;
        internal Guid Id;

        public const string UnknownName = "- -";

        public class Cache
        {
            private IReadOnlyDictionary<Guid, string> _store;
            public Cache(IReadOnlyDictionary<Guid, string> store)
            {
                if (store == null) throw new ArgumentNullException("store");
                _store = store;
            }

            public User this[Guid id] { get
                {
                    string name;
                    if (_store.TryGetValue(id, out name))
                        return new User { Name = name, Id = id };
                    return new User { Name = UnknownName, Id = id };
                } }
        }
    }
}