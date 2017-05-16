using NUnit.Framework;
using System;
using System.Linq;
using System.Transactions;

namespace PostgresLol
{
    [TestFixture, TestOf("PostgreSql as backend for readmodel")]
    class Tests
    {
        private TransactionScope _testScope;

        [SetUp]
        public void Scope()
        {
            if (_testScope != null)
                _testScope.Dispose();
            _testScope = new TransactionScope();
        }

        //[TearDown]
        //public void Commit()
        //{
        //    _testScope.Complete();
        //}

        [Test, TestOf("Basic write/read")]
        public void RegistrationCanBeCreatedTemporallyDecoupledFromUserCreatingIt()
        {
            var jamesId = Guid.NewGuid();
            var newRegistrationId = Guid.NewGuid();
            var p = new RegistrationEventHandler();
            p.RegistrationRegistered(newRegistrationId, "Some text", jamesId);
            p.UserCreated("James", jamesId);
            var rs = new RegistrationReadService();

            var result = rs.SearchRegistrations(default(RegistrationQuery));
             
            Assert.AreEqual(1, result.Count());
            var r = result.First();
            Assert.AreEqual("James", r.Responsible.Name);
            Assert.AreEqual("Some text", r.Description);
            Assert.That(r.Created, Is.InRange(DateTime.Now.AddSeconds(-2), DateTime.Now));
            Assert.That(r.LatestChange, Is.InRange(DateTime.Now.AddSeconds(-2), DateTime.Now));
            Assert.AreEqual(0, r.Assignees.Length);
            Assert.AreEqual(0, r.Comments.Length);
            Assert.AreEqual(RegistrationState.Open, r.State);
        }

        [Test, TestOf("QueryObject")]
        public void MyRegistrationsCanbeRetrieved()
        {            
            Guid jamesId = Guid.NewGuid();
            Guid regAId = Guid.NewGuid(), regBId = Guid.NewGuid(), regCId = Guid.NewGuid();
            var otherRegistrationIds = Enumerable.Repeat(0, 5).Select(x=>Guid.NewGuid());
            var p = new RegistrationEventHandler();
            //james creates 'a', and is assigned to 'b' and 'c'
            p.RegistrationRegistered(regAId,"Some text A", jamesId);
            p.RegistrationRegistered(regBId, "Some text B", responsibleUserId: Guid.NewGuid());
            p.UserAssignedRegistration(regBId, jamesId); 
            p.RegistrationRegistered(regCId, "Some text C", responsibleUserId: Guid.NewGuid());        
            p.UserAssignedRegistration(regCId, jamesId); 
            foreach(var regId in otherRegistrationIds)
            {   //other registrations does not concern james
                p.RegistrationRegistered(regId, "other", responsibleUserId: Guid.NewGuid());
                p.UserAssignedRegistration(regId, Guid.NewGuid());
            }
            var rs = new RegistrationReadService();

            var result = rs.SearchRegistrations(new RegistrationQuery { ForUser = jamesId });

            Assert.AreEqual(3, result.Count());
            var a = result.Single(r => r.Id == regAId);
            Assert.AreEqual(jamesId, a.Responsible.Id);
            Assert.IsEmpty(a.Assignees);
            var b = result.Single(r => r.Id == regBId);
            Assert.AreNotEqual(jamesId, b.Responsible.Id);
            CollectionAssert.Contains(b.Assignees.Select(u=>u.Id), jamesId);
            var c = result.Single(r => r.Id == regCId);
            Assert.AreNotEqual(jamesId, c.Responsible.Id);
            CollectionAssert.Contains(c.Assignees.Select(u => u.Id), jamesId);
        }
    }
}
