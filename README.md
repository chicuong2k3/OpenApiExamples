# Open Api

Install package Swashbuckle.AspNetCore
	- Generate an OpenAPI specification for from API
	- Wrap Swagger UI and provide a embedded version of it

```c#
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setupAction => 
{
	setupAction.SwaggerDoc("v1", new OpenApiInfo 
	{ 
		Title = "My API", 
		Version = "1",
		Description = "A simple example ASP.NET Core Web API",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "Your Email",
            Url = new Uri("https://example.com"),
        },
		License = new OpenApiLicense
        {
            Name = "Use under LICX",
            Url = new Uri("https://example.com/license"),
        },
		TermsOfService = new Uri("https://example.com/tos")
	});

	var xmlCommentsFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlCommentsFileFullPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName);
	setupAction.IncludeXmlComments(xmlCommentsFileFullPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(setupAction => 
	{
		// v1 match the string in setupAction.SwaggerDoc
		setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
		setupAction.RoutePrefix = "";
	});
}
```

**XML Comments**

Properties -> Build -> Output -> Documentation file

**Add summary for model classes and their properties**
**Add attributes for model class's properties**

```c#
/// <summary>
/// Get a book by id
/// </summary>
/// <param name="id">The id of the book you want to get</param>
/// <returns>A book with the specified id</returns>
/// <remarks>
/// Sample request:
///    GET /books/1
/// </remarks>
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BookResponse))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public IActionResult Get(int id)
{
    return Ok();
}
```

## Api Analyzers

Install package Microsoft.AspNetCore.Mvc.Api.Analyzers

```xml
<IncludeOpenAPIAnalyzers>true</IncludeOpenAPIAnalyzers>
```

## Globally Apply Attributes

```c#
builder.Services.AddControllers(configure => 
{
	configure.ReturnHttpNotAcceptable = true;

	configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
	configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status406NotAcceptable));
	configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));
})
.AddNewtonsoftJson(setupAction => 
{
	setupAction.SerializerSettings.ContractResolver 
		= new CamelCasePropertyNamesContractResolver();
})
.AddXmlDataContractSerializerFormatters();
```

## Api Conventions

```c#
[ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Get))]
public IActionResult Get(int id)
{
	return Ok();
}
```

```c#
// At controller level
[ApiConventionType(typeof(DefaultApiConventions))]

// At assembly level
[assembly: ApiConventionType(typeof(DefaultApiConventions))]
```

### Custom Conventions

```c#
public static class CustomConventions
{
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesDefaultResponseType]
	[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
	public static void Insert(
		[ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
		[ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
		object model
	)
	{
		
	}
}
```

## Content Negotiation

### Output

```c#
[Produces("application/json")]
```

### Input

```c#
[Consumes("application/json", "application/xml")]
```

## Input and Output Description

### Content Negotiation with Vendor-specific Media Types

```c#
[HttpGet("/customers")]
[RequestHeaderMatchesMediaType("Accept", "application/json")]
public IActionResult GetCustomer(int id)
{
	return Ok();
}

[HttpGet("/customers")]
[RequestHeaderMatchesMediaType("Accept", "application/customerwithfullname+json")]
[Produces("application/customerwithfullname+json")]
public IActionResult GetCustomerWithFullName(int id)
{
	return Ok();
}
```

```c#
[AttributeUge(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
{
	public readonly MediaTypeCollection _mediaTypes = new();
	public readonly string _requestHeaderToMatch;

	public RequestHeaderMatchesMediaTypeAttribute(
		string requestHeaderToMatch, 
		string mediaType,
		params string[] otherMediaTypes)
	{
		_requestHeaderToMatch = requestHeaderToMatch
               ?? throw new ArgumentNullException(nameof(requestHeaderToMatch));

        // check if the inputted media types are valid media types
        // and add them to the _mediaTypes collection                     

        if (MediaTypeHeaderValue.TryParse(mediaType,
            out MediaTypeHeaderValue? parsedMediaType))
        {
            _mediaTypes.Add(parsedMediaType);
        }
        else
        {
            throw new ArgumentException("Argument can not be null or empty.", 
                nameof(mediaType));
        }

        foreach (var otherMediaType in otherMediaTypes)
        {
            if (MediaTypeHeaderValue.TryParse(otherMediaType,
               out MediaTypeHeaderValue? parsedOtherMediaType))
            {
                _mediaTypes.Add(parsedOtherMediaType);
            }
            else
            {
                throw new ArgumentException("Argument can not be null or empty.", 
                    nameof(otherMediaTypes));
            }
        }
	}

	public int Order { get; }

	public bool Accept(ActionConstraintContext context)
	{
		var requestHeaders = context.RouteContext.HttpContext.Request.Headers;
		if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
        {
			return false;
		}

		var parsedRequestMediaType = new MediaType(requestHeaders[_requestHeaderToMatch]);

        // if one of the media types matches, return true
        foreach (var mediaType in _mediaTypes)
        {
            var parsedMediaType = new MediaType(mediaType);
            if (parsedRequestMediaType.Equals(parsedMediaType))
            {
                return true;
            }
        }
        return false;
	}
}
```

```c#
builder.Services.AddSwaggerGen(setupAction => 
{
	setupAction.ResolveConflictingActions(apiDescriptions => 
	{
		var firstDescription = apiDescriptions.First();
		var secondDescription = apiDescriptions.ElementAt(1);

		firstDescription.SupportResponseTypes.AddRange(
			seconDescription.SupportResponseTypes.Where(x => x.StatusCode == 200)
		);

		return firstDescription;
	});
});
```

### Schema Variation by Output

Give a name in Http Verb attribute

```c#
public class GetCustomerFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		if (operation.OperationId != "GetCustomer")
		{
			return;
		}

		if (operation.Responses.Any(res => res.Key == StatusCodes.Status200OK.ToString()))
		{
			var response = operation.Responses[StatusCodes.Status200OK.ToString()];

			response.Content.Add("application/customerwithfullname+json", new OpenApiMediaType
			{
				Schema = context.SchemaGenerator.GenerateSchema(
					typeof(CustomerWithFullNameResponse),
					context.SchemaRepository
				)
			});
		}


	}
}
```

```c#
builder.Services.AddSwaggerGen(setupAction => 
{
	setupAction.OperationFilter<GetCustomerFilter>();
}
```

### Schema Variation by Input

```c#
[HttpPost("/customers")]
public IActionResult Create([FromBody] Customer customer)
{
	return Ok();
}

[HttpPost("/customers")]
[Consumes("application/customerwithage+json")]
public IActionResult Create([FromBody] CustomerWithAge customer)
{
	return Ok();
}
```

```c#
public class CreateCustomerFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		if (operation.OperationId != "CreateCustomer")
		{
			return;
		}


		operation.RequestBody.Content.Add("application/customerwithage+json", new OpenApiMediaType
		{
			Schema = context.SchemaGenerator.GenerateSchema(
				typeof(CustomerWithAge),
				context.SchemaRepository
			)
		});


	}
}
```

## Versioning

Install package Microsoft.AspNetCore.Mvc.Versioning
Install package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer

```c#

var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider()
											.GetRequiredService<IApiVersionDescriptionProvider>();


builder.Services.AddSwaggerGen(setupAction => 
{

	foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
	{
		setupAction.SwaggerDoc($"{description.GroupName}", new OpenApiInfo 
		{ 
			Title = "My API", 
			Version = description.ApiVersion.ToString(),
			Description = "A simple example ASP.NET Core Web API",
			Contact = new OpenApiContact
			{
				Name = "Your Name",
				Email = "Your Email",
				Url = new Uri("https://example.com"),
			},
			License = new OpenApiLicense
			{
				Name = "Use under LICX",
				Url = new Uri("https://example.com/license"),
			},
			TermsOfService = new Uri("https://example.com/tos")
		});
	})

	setupAction.DocInclusionPredicate((docName, apiDescription) => 
	{
		if (!apiDescription.TryGetMethodInfo(out MethodInfo methodInfo))
		{
			return false;
		}

		var actionApiVersionModel = apiDescription.ActionDescriptor.GetApiVersionModel(
										ApiVersionMapping.Explicit |
										ApiVersionMapping.Implicit
									);

		if (actionApiVersionModel is null)
		{
			return true;
		}

		if (actionApiVersionModel.DeclaredApiVersions.Any())
		{
			return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
		}

		return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
	});

});	

builder.Services.AddApiVersioning(setupAction =>
{
    setupAction.AssumeDefaultVersionWhenUnspecified = true;
    setupAction.DefaultApiVersion = new ApiVersion(1, 0);
    setupAction.ReportApiVersions = true;

	// by default, the combined version reader is used

    setupAction.ApiVersionReader = new HeaderApiVersionReader("api-version"); 
    // setupAction.ApiVersionReader = new QueryStringApiVersionReader("api-version"); 
    // setupAction.ApiVersionReader = new MediaTypeApiVersionReader(); 
}).AddMvc();
//.AddMvc(options =>
// {
//     options.Conventions.Controller<EcommerceApp.Api.Controllers.V1.CategoriesController>()
//     .HasDeprecatedApiVersion(new ApiVersion(1, 0));
//     options.Conventions.Controller<EcommerceApp.Api.Controllers.V2.CategoriesController>()
//     .HasApiVersion(new ApiVersion(2, 0));
// });

builder.Services.AddVersionedApiExplorer(setupAction =>
{
	setupAction.GroupNameFormat = "'v'VV";
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(setupAction => 
	{
		foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
		{
			setupAction.SwaggerEndpoint(
				"/swagger/{description.GroupName}/swagger.json", 
				description.GroupName.ToUpperInvariant()
			);
			setupAction.RoutePrefix = "";
		}
	});
}
```

```c#
//[Route(api/v{version:apiVersion}/[controller])]
[ApiVersion("2.0")]
[ApiExplorerSettings(GroupName = "v2")]
[ApiController]
public class SomeController {}
```

## Protecting API

- Security Scheme type:
	- HTTP authentication schemes: http
	- API keys: apiKey
	- OAuth2: oauth2
	- OpenIDConnect: openIdConnect


```c#
public class BasicAuthenticationHandler 
	: AuthenticationHandler<AuthenticationSchemeOptions>
{
	public BasicAuthenticationHandler(
		IOptionsMonitor<BasicAuthenticationHandler> options,
		UrlEncoder encoder,
		ISystemClock systemClock
	) : base(options, encoder, systemClock)
	{
		
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey("Authorization"))
		{
			return Task.FromResult(AuthenticateResult.Fail("Authorization header is required."));
		}

		try
		{
			var authenticationHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

			var credentials = Encoding.UT8.GetString(Convert.FromBase64String(authenticationHeader.Parameter));

			var username = credentials[0];
			var password = credentials[1];

			if (username == "username" || password == "password")
			{
				var claims = new Claim[]
				{
					new Claim(ClaimTypes.NameIdentifier, username)
				};

				var identity = new ClaimsIdentity(claims, Scheme.Name);
				var principal = new ClaimsPrincipal(identity);
				var authenticationTicket = new AuthenticationTicket(principal, Scheme.Name);

				return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
			}

			return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
		}
		catch(Exception)
		{
			return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
		}
	}
}
```

```c#
builder.Services.AddControllers(configure => 
{
	configure.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
	configure.Filters.Add(new AuthorizeFilter());
});

builder.Services.AddAuthentication("BasicAuthentication")
	.AddScheme<AuthenticationSchemeOptions, 
			BasicAuthenticationHandler>("BasicAuthentication", null);

builder.Services.AddSwaggerGen(setupAction => 
{
	setupAction.AddSecurityDefinition("basic", new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.Http,
		Scheme = "basic", // this is case sensitive
		Description = "Basic authentication scheme"
	});

	setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "basic"
				}
			},
			new string[] {}
		}
	});
});
```

## Advanced Customization

### Enriching Comments with Markdown

```c#
app.UseSwaggerUI(setupAction =>
{
    setupAction.DefaultModelExpandDepth(2);
    setupAction.DefaultModelRendering(ModelRendering.Model);
    setupAction.DocExpansion(DocExpansion.None);
    setupAction.DisplayOperationId();
    setupAction.EnableDeepLinking(); // #/author/getauthors will expand and scroll to the GetAuthors operation
});
```

### Customizing the Swagger UI

```c#
setupAction.InjectStylesheet("/swagger-ui/custom.css");
setupAction.InjectJavascript("/swagger-ui/custom.js");

// change build action to Embedded Resource
setupAction.IndexStream = () => GetType().Assembly
	.GetManifestResourceStream("EcommerceApp.Api.Swagger.index.html");
```