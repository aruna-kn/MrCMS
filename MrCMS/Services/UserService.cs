using System;
using System.Collections.Generic;
using System.Web;
using MrCMS.Entities.Multisite;
using MrCMS.Entities.People;
using MrCMS.Helpers;
using MrCMS.Paging;
using MrCMS.Website;
using NHibernate;
using NHibernate.Criterion;

namespace MrCMS.Services
{
    public class UserService : IUserService
    {
        private readonly ISession _session;
        private readonly ISiteService _siteService;

        public UserService(ISession session, ISiteService siteService)
        {
            _session = session;
            _siteService = siteService;
        }

        public void AddUser(User user)
        {
            _session.Transact(session =>
                                  {
                                      var site = _siteService.GetCurrentSite();

                                      if (user.Sites != null)
                                          user.Sites.Add(site);

                                      if (site.Users != null)
                                          site.Users.Add(user);

                                      session.Save(user);
                                      session.Update(site);
                                  });
        }

        public void SaveUser(User user)
        {
            _session.Transact(session => session.Update(user));
        }

        public User GetUser(int id)
        {
            return _session.Get<User>(id);
        }

        public IList<User> GetAllUsers()
        {
            return _session.QueryOver<User>().Cacheable().List();
        }

        public IPagedList<User> GetAllUsersPaged(int page)
        {
            return _session.QueryOver<User>().Paged(page, 10);
        }

        public User GetUserByEmail(string email)
        {
            string trim = email.Trim();
            return
                _session.QueryOver<User>().Where(user => user.Email==trim).Cacheable().
                    SingleOrDefault();
        }

        public User GetUserByResetGuid(Guid resetGuid)
        {
            return
                _session.QueryOver<User>()
                    .Where(user => user.ResetPasswordGuid == resetGuid && user.ResetPasswordExpiry >= CurrentRequestData.Now)
                    .Cacheable().SingleOrDefault();
        }

        public User GetCurrentUser(HttpContextBase context)
        {
            return context.User != null ? GetUserByEmail(context.User.Identity.Name) : null;
        }

        public void DeleteUser(User user)
        {
            _session.Transact(session =>
                                  {
                                      user.OnDeleting(session);
                                      session.Delete(user);
                                  });
        }

        /// <summary>
        /// Checks to see if the supplied email address is unique
        /// </summary>
        /// <param name="email"></param>
        /// <param name="id">The id of user to exlcude from check. Has to be string because of AdditionFields on Remote property</param>
        /// <returns></returns>
        public bool IsUniqueEmail(string email, int? id = null)
        {
            if (id.HasValue)
            {
                return _session.QueryOver<User>().Where(u => u.Email == email && u.Id != id.Value).RowCount() == 0;
            }
            return _session.QueryOver<User>().Where(u => u.Email == email).RowCount() == 0;
        }

        /// <summary>
        /// Gets a count of active users
        /// </summary>
        /// <returns></returns>
        public int ActiveUsers()
        {
            return _session.QueryOver<User>().Where(x => x.IsActive).Cacheable().RowCount();
        }

        /// <summary>
        /// Gets a count of none active users
        /// </summary>
        /// <returns></returns>
        public int NonActiveUsers()
        {
            return _session.QueryOver<User>().WhereNot(x => x.IsActive).Cacheable().RowCount();
        }
    }
}