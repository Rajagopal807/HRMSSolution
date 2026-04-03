using HRMS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace HRMS.Web.Session
{
    public class HttpUserSession : IUserSession
    {
        private readonly HttpSessionStateBase _session;

        public HttpUserSession(HttpSessionStateBase session)
        {
            _session = session;
        }

        public string UserId => _session["UserId"]?.ToString();

        public string UserName => _session["UserName"]?.ToString();

        public string Role => _session["Role"]?.ToString();

        public bool IsAuthenticated => UserId != null;

        public DateTime? LastActivity => _session["LastActivity"] as DateTime?;

        public void Clear()
        {
            _session.Clear();
            _session.Abandon();
        }

        public void Create(string userId, string userName, string role)
        {
            _session["UserId"] = userId;
            _session["UserName"] = userName;
            _session["Role"] = role;
            _session["LastActivity"] = DateTime.UtcNow;
        }

        public void UpdateActivity()
        {
            _session["LastActivity"] = DateTime.UtcNow;
        }
    }
}