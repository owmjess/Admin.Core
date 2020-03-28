using System.Threading.Tasks;
using AutoMapper;
using Admin.Core.Common.Auth;
using Admin.Core.Model.Input;
using Admin.Core.Model.Output;
using Admin.Core.Model.Admin;
using Admin.Core.Repository.Admin;
using Admin.Core.Service.Admin.LoginLog.Input;
using Admin.Core.Service.Admin.LoginLog.Output;
using Microsoft.AspNetCore.Http;

namespace Admin.Core.Service.Admin.LoginLog
{	
	public class LoginLogService : ILoginLogService
    {
        private readonly IUser _user;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _context;
        private readonly ILoginLogRepository _loginLogRepository;
        public LoginLogService(
            IUser user,
            IMapper mapper,
            IHttpContextAccessor context,
            ILoginLogRepository loginLogRepository
        )
        {
            _user = user;
            _mapper = mapper;
            _context = context;
            _loginLogRepository = loginLogRepository;
        }

        public async Task<IResponseOutput> PageAsync(PageInput<LoginLogEntity> input)
        {
            var userName = input.Filter?.CreatedUserName;

            var list = await _loginLogRepository.Select
            .WhereIf(userName.NotNull(), a => a.CreatedUserName.Contains(userName))
            .Count(out var total)
            .OrderByDescending(true, c => c.Id)
            .Page(input.CurrentPage, input.PageSize)
            .ToListAsync<LoginLogListOutput>();

            var data = new PageOutput<LoginLogListOutput>()
            {
                List = list,
                Total = total
            };
            
            return ResponseOutput.Ok(data);
        }

        public async Task<IResponseOutput<long>> AddAsync(LoginLogAddInput input)
        {
            var res = new ResponseOutput<long>();

            input.IP = _user.IP;

            string ua = _context.HttpContext.Request.Headers["User-Agent"];
            var client = UAParser.Parser.GetDefault().Parse(ua);
            var device = client.Device.Family;
            device = device.ToLower() == "other" ? "" : device;
            input.Browser = client.UA.Family;
            input.Os = client.OS.Family;
            input.Device = device;
            input.BrowserInfo = ua;

            var entity = _mapper.Map<LoginLogEntity>(input);
            var id = (await _loginLogRepository.InsertAsync(entity)).Id;

            return id > 0 ? res.Ok(id) : res;
        }
    }
}
