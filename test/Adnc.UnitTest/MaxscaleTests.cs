﻿using System;
using System.Threading.Tasks;
using System.Linq;
using Xunit;
using Autofac;
using Xunit.Abstractions;
using Adnc.UnitTest.Base;
using Adnc.UnitTest.Fixtures;
using Adnc.Core.Shared;
using Adnc.Cus.Core.Entities;
using Adnc.Core.Shared.IRepositories;
using Adnc.Infr.Common;
using Adnc.Infr.Common.Helper;
using System.Collections.Generic;

namespace Adnc.UnitTests
{
    public class MaxscaleTests: IClassFixture<MaxscaleDbcontextFixture>
    {
        private readonly ITestOutputHelper _output;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserContext _userContext;
        private readonly IEfRepository<Customer> _cusRsp;
        private readonly IEfRepository<CusFinance> _cusFinanceRsp;
        private readonly IEfRepository<CusTransactionLog> _cusLogsRsp;
        private MaxscaleDbcontextFixture _fixture;

        public MaxscaleTests(MaxscaleDbcontextFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _unitOfWork = _fixture.Container.Resolve<IUnitOfWork>();
            _userContext = _fixture.Container.Resolve<UserContext>();
            _cusRsp = _fixture.Container.Resolve<IEfRepository<Customer>>();
            _cusFinanceRsp = _fixture.Container.Resolve<IEfRepository<CusFinance>>();
            _cusLogsRsp = _fixture.Container.Resolve<IEfRepository<CusTransactionLog>>();

            Initialize();
        }

        private void Initialize()
        {           
            _userContext.ID = 1600000000000;
            _userContext.Account = "alpha2008";
            _userContext.Name = "余小猫";
        }

        [Fact]
        public async void TestReadFromWirteDb()
        {
            var result = await InsertCustomer();
            var cusFinance = await InsertCusFinance(result.ID);

            var cus = await _cusRsp.FetchAsync(c => c, c => c.ID == result.ID, writeDb: true);
            Assert.NotNull(cus);

            await _cusFinanceRsp.DeleteAsync(result.ID);
            await _cusRsp.DeleteAsync(result.ID);
        }

        [Fact]
        public async void TestDapperReadFromWirteDb()
        {
            var result = await InsertCustomer();
            var cusFinance = await InsertCusFinance(result.ID);

            var cus = await _cusRsp.QueryAsync<Customer>("select * from customer where id=@id", new { id = result.ID }, writeDb: true);
            Assert.NotNull(cus);

            await _cusFinanceRsp.DeleteAsync(result.ID);
            await _cusRsp.DeleteAsync(result.ID);
        }

        [Fact]
        public async void TestInsertRange()
        {
            var list = await InsertRangeCustomer(10);
            var ids = list.Select(c => c.ID).ToArray();
            var cusCount = await _cusRsp.CountAsync(c => ids.Contains(c.ID), writeDb: true);
            var cusFinanceCount = await _cusFinanceRsp.CountAsync(c => ids.Contains(c.ID), writeDb: true);

            Assert.Equal(10, cusCount);
            Assert.Equal(10, cusFinanceCount);

            await _cusFinanceRsp.DeleteRangeAsync(c => ids.Contains(c.ID));
            await _cusRsp.DeleteRangeAsync(c => ids.Contains(c.ID));
        }

        [Fact]
        public async void TestUpdate()
        {
            var newRealName = "测试用户";
            var newNickname = "测试";
            long id = 0;

            try
            {
                _unitOfWork.BeginTransaction();

                var result = await InsertCustomer();
                id = result.ID;

                result.Realname = newRealName;
                result.Nickname = newNickname;
                await _cusRsp.UpdateAsync(result, c => c.Realname, c => c.Nickname);

                _unitOfWork.Commit();
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
            }
            finally
            {
                _unitOfWork.Dispose();
            }


            var newCus = await _cusRsp.FindAsync(id, writeDb: true);
            Assert.Equal(newRealName, newCus.Realname);
            Assert.Equal(newNickname, newCus.Nickname);
        }

        [Fact]
        public async void TestUpdateFullColumn()
        {
            var newRealName = "测试用户";
            var newNickname = "测试";
            long id = 0;

            try
            {
                _unitOfWork.BeginTransaction();

                var result = await InsertCustomer();
                id = result.ID;

                result.Realname = newRealName;
                result.Nickname = newNickname;
                await _cusRsp.UpdateAsync(result);

                _unitOfWork.Commit();
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
            }
            finally
            {
                _unitOfWork.Dispose();
            }


            var newCus = await _cusRsp.FindAsync(id, writeDb: true);
            Assert.Equal(newRealName, newCus.Realname);
            Assert.Equal(newNickname, newCus.Nickname);
        }

        [Fact]
        public async void TestUpdateRange()
        {
            var list = await InsertRangeCustomer(10);
            var ids = list.Select(c => c.ID).ToArray();
            var newRealName = "测试用户001";
            var newNickname = "测试";

            await _cusRsp.UpdateRangeAsync(c => ids.Contains(c.ID), c => new Customer { Realname = newRealName, Nickname = newNickname });

            var newCus = await _cusRsp.FindAsync(list[0].ID, writeDb: true);
            
            Assert.Equal(newRealName, newCus.Realname);
            Assert.Equal(newNickname, newCus.Nickname);

            await _cusFinanceRsp.DeleteRangeAsync(c => ids.Contains(c.ID));
            await _cusRsp.DeleteRangeAsync(c=>ids.Contains(c.ID));
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        /// <returns></returns>
        private async Task<List<Customer>> InsertRangeCustomer(int rows)
        {
            var list = new List<Customer>();
            for (int i = 0; i < rows; i++)
            {
                var id = IdGenerater.GetNextId(IdGenerater.DatacenterId, IdGenerater.WorkerId);
                var customer = new Customer() { ID = id, Account = "alpha2008", Nickname = IdGenerater.GetNextId().ToString(), Realname = IdGenerater.GetNextId().ToString() };
                customer.CusFinance = new CusFinance { Account = "alpha2008", ID = id, Balance = 0 };
                list.Add(customer);
            }
            await _cusRsp.InsertRangeAsync(list);
            
            return list;
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        /// <returns></returns>
        private async Task<Customer> InsertCustomer()
        {
            var id = IdGenerater.GetNextId(IdGenerater.DatacenterId, IdGenerater.WorkerId);
            var customer = new Customer() { ID = id, Account = "alpha2008", Nickname = IdGenerater.GetNextId().ToString(), Realname = IdGenerater.GetNextId().ToString() };
            await _cusRsp.InsertAsync(customer);
            return customer;
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<CusFinance> InsertCusFinance(long id)
        {
            var cusFinance = new CusFinance { Account = "alpha2008", ID = id, Balance = 0 };
            await _cusFinanceRsp.InsertAsync(cusFinance);
            return cusFinance;
        }
    }
}
