﻿/* 
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial 
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Data;
using System.Linq;
using AutoMapper;
using Dapper;
using MiningCore.Persistence.Model;
using MiningCore.Persistence.Repositories;

namespace MiningCore.Persistence.Postgres.Repositories
{
    public class StatsRepository : IStatsRepository
    {
        public StatsRepository(IMapper mapper)
        {
            this.mapper = mapper;
        }

        private readonly IMapper mapper;

        public void Insert(IDbConnection con, IDbTransaction tx, PoolStats stats)
        {
            var mapped = mapper.Map<Entities.PoolStats>(stats);

            var query = "INSERT INTO poolstats(poolid, connectedminers, poolhashrate, sharespersecond, " +
                        "validsharesperminute, invalidsharesperminute, created) " +
                        "VALUES(@poolid, @connectedminers, @poolhashrate, @sharespersecond, @validsharesperminute, " +
                        "@invalidsharesperminute, @created)";

            con.Execute(query, mapped, tx);
        }

        public PoolStats[] PageStatsBetween(IDbConnection con, string poolId, DateTime start, DateTime end, int page, int pageSize)
        {
            var query = "SELECT * FROM poolstats WHERE poolid = @poolId AND created >= @start AND created <= @end " +
                        "ORDER BY created DESC OFFSET @offset FETCH NEXT (@pageSize) ROWS ONLY";

            return con.Query<Entities.PoolStats>(query, new { poolId, start, end, offset = page * pageSize, pageSize })
                .Select(mapper.Map<PoolStats>)
                .ToArray();
        }

        public PoolStats[] GetHourlyStatsBetween(IDbConnection con, string poolId, DateTime start, DateTime end)
        {
            var query = "SELECT date_trunc('hour', created) AS created, " +
                        "   AVG(poolhashrate) AS poolhashrate, " +
                        "   CAST(AVG(connectedminers) AS BIGINT) AS connectedminers " +
                        "FROM poolstats " +
                        "WHERE poolid = @poolId AND created >= @start AND created <= @end " +
                        "GROUP BY date_trunc('hour', created) " +
                        "ORDER BY created;";

            return con.Query<Entities.PoolStats>(query, new { poolId, start, end })
                .Select(mapper.Map<PoolStats>)
                .ToArray();
        }
    }
}
