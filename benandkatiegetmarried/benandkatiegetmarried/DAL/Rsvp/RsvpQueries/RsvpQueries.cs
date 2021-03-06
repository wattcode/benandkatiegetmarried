﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using benandkatiegetmarried.Models;
using PetaPoco;

namespace benandkatiegetmarried.DAL.Rsvp.RsvpQueries
{
    public class RsvpQueries : IRsvpQueries
    {
        private IWeddingDatabase _db;

        public RsvpQueries(IWeddingDatabase db)
        {
            _db = db;
        }
        public IEnumerable<Models.Rsvp> GetAll()
        {
            IEnumerable<Models.Rsvp> result;
            using(var uow = _db.GetTransaction())
            {
                result = _db.Query<Models.Rsvp>("");
                uow.Complete();
            }
            return result;
        }

        public IEnumerable<Models.Rsvp> GetByGuestIds(IEnumerable<Guid> guestIds)
        {
            IEnumerable<Models.Rsvp> result;
            using (var uow = _db.GetTransaction())
            {
                result = _db.Query<Models.Rsvp>(@"SELECT Rsvps.*
                                                FROM core.RsvpResponses AS res
                                                    INNER JOIN core.Rsvps
	                                                    ON Rsvps.Id = res.RsvpId
                                                WHERE res.GuestId IN (@0)", guestIds).ToList();
                uow.Complete();
            }
            return result;
        }
    }
}
