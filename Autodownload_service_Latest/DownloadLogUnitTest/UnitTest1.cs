using AutodownloadService;
using AutodownloadService.Interface;
using AutodownloadService.Interface.Unit;
using AutodownloadService.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;

namespace DownloadLogUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void DownloadTest()
        {
            IAutodownload _autodownload = new AutodownloadImpl();
            List<RawDatum> rawData = _autodownload.DownloadFromEpush();
            if (rawData.Count > 0)
                _autodownload.CreateAtndFile(rawData);

            Assert.IsNotNull(rawData);
        }

        [TestMethod]
        public void PostingTest()
        {
            PostDataToDBUnit postDataToDB = new PostDataToDBUnit();
            try
            {
                postDataToDB.PostData();
            }
            catch (Exception ex)
            {
                Assert.Fail("PostingTest failed: " + ex.Message);
            }
        }

        [TestMethod]
        public void ComputeTest()
        {
            Computation computation = new Computation();
            try
            {
                computation.ComputeAttendance();
            }
            catch (Exception ex)
            {
                Assert.Fail("PostingTest failed: " + ex.Message);
            }
        }
    }
}
