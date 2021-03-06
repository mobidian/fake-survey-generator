﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FakeSurveyGenerator.Application.Common.Interfaces;
using FakeSurveyGenerator.Application.Surveys.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace FakeSurveyGenerator.Application.Surveys.Queries.GetSurveyDetail
{
    public class GetSurveyDetailWithEntityFrameworkQueryHandler : IRequestHandler<GetSurveyDetailQuery, SurveyModel>
    {
        private readonly ISurveyContext _surveyContext;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public GetSurveyDetailWithEntityFrameworkQueryHandler(ISurveyContext surveyContext, IMapper mapper, IDistributedCache cache)
        {
            _surveyContext = surveyContext ?? throw new ArgumentNullException(nameof(surveyContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<SurveyModel> Handle(GetSurveyDetailQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"FakeSurveyGenerator:Survey:{request.Id.ToString()}";

            var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrWhiteSpace(cachedValue))
                return System.Text.Json.JsonSerializer.Deserialize<SurveyModel>(cachedValue);

            var survey = await _surveyContext.Surveys
                .Include(s => s.Options)
                .ProjectTo<SurveyModel>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
                

            if (survey == null)
                throw new KeyNotFoundException();

            await _cache.SetStringAsync(cacheKey, survey.ToString(), new DistributedCacheEntryOptions
            {
                SlidingExpiration = new TimeSpan(1, 0, 0)
            }, cancellationToken);

            return survey;
        }
    }
}
