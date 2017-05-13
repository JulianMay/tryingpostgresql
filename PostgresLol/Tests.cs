using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace PostgresLol
{
    [TestFixture]
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


        [Test]
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

        [Test]
        public void MyRegistrationsCanbeRetrieved()
        {
            //Returns registrations that specified user is responsible for or assigned to
            var jamesId = Guid.NewGuid();
            var regAId = Guid.NewGuid();
            var regBId = Guid.NewGuid();
            var regCId = Guid.NewGuid();
            var otherRegistrationIds = Enumerable.Repeat(Guid.NewGuid(), 5);
            var p = new RegistrationEventHandler();
            //james creates 'a', and is assigned to 'b' and 'c'
            p.RegistrationRegistered(regAId,"Some text A", jamesId);
            p.RegistrationRegistered(regAId, "Some text B", responsibleUserId: Guid.NewGuid());
            p.UserAssignedRegistration(regBId, jamesId); 
            p.RegistrationRegistered(regAId, "Some text C", responsibleUserId: Guid.NewGuid());        
            p.UserAssignedRegistration(regBId, jamesId); 
            foreach(var regId in otherRegistrationIds)
            {   //other registrations does not concern james
                p.RegistrationRegistered(regId, "other", responsibleUserId: Guid.NewGuid());
                p.UserAssignedRegistration(regId, Guid.NewGuid());
            }
            var rs = new RegistrationReadService();

            var result = rs.SearchRegistrations(new RegistrationQuery { ForUser = jamesId });

            Assert.AreEqual(3, result.Count());
            var a = result.Single(r => r.Id == regAId);
            Assert.AreEqual(jamesId, a.ResponsibleId);
            Assert.IsEmpty(a.Assignees);
            var b = result.Single(r => r.Id == regBId);
            Assert.AreNotEqual(jamesId, b.ResponsibleId);
            CollectionAssert.Contains(b.Assignees.Select(u=>u.Id), jamesId);
            var c = result.Single(r => r.Id == regCId);
            Assert.AreNotEqual(jamesId, c.ResponsibleId);
            CollectionAssert.Contains(c.Assignees.Select(u => u.Id), jamesId);
        }
    }
}
