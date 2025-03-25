using Microsoft.AspNetCore.Mvc; // // For IFormFile
using System.Text;
using System.Text.Json;
using DataInfo.Models;
using System.Diagnostics;
using System.Web.Mvc;

namespace DataInfo.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Extensions.Hosting;
    using System.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Supabase.Interfaces;
    using Supabase.Gotrue;

    namespace YourNamespace.Controllers
    {
       
        public class HomeController : Controller 
        {
            private readonly ApplicationDbContext _applicationDbContext;
            private readonly IWebHostEnvironment _env;
            private readonly HttpClient _httpClient;
            private readonly string _apiBaseUrl;
            private readonly Supabase.Client _supabaseClient;



            public HomeController(ApplicationDbContext applicationDbContext, IWebHostEnvironment env, IConfiguration configuration, IHttpClientFactory httpClientFactory, Supabase.Client supabaseClient)
            {
                _applicationDbContext = applicationDbContext;
                _env = env;
                _httpClient = httpClientFactory.CreateClient();
                _apiBaseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl")
            ?? throw new InvalidOperationException("API base URL is not configured.");
                _supabaseClient = supabaseClient;
            }

            [HttpPost]
            public async Task<IActionResult> Web_Api( DataTableRequest dataTableRequest)
            {
                try
                {
                    string jsonPayload = JsonSerializer.Serialize(dataTableRequest);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var searchValue = dataTableRequest.Search != null ? Uri.EscapeDataString(dataTableRequest.Search.Value ?? "") : "";
                    var searchRegex = dataTableRequest.Search != null ? dataTableRequest.Search.Regex.ToString().ToLower() : "false";
                    var searchQuery = $"&search.value={searchValue}&search.regex={searchRegex}";

                    var order = dataTableRequest.Order?.FirstOrDefault();
                    var orderQuery = order != null
                        ? $"&order[0].column={order.Column}&order[0].dir={Uri.EscapeDataString(order.Dir)}"
                        : "";

                    var url = $"{_apiBaseUrl}/api/Home/GetAll?draw={dataTableRequest.Draw}&start={dataTableRequest.Start}&length={dataTableRequest.Length}{searchQuery}{orderQuery}";

                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<DataTableResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    string jsonResult = JsonSerializer.Serialize(result);
                    return new ContentResult
                    {
                        Content = jsonResult,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (HttpRequestException ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 400
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> CreateUser(UserData user, IFormFile file)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        string newFileName = fileName + "_" + Guid.NewGuid() + extension;

                        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".JPG", ".JPEG" };
                        if (allowedExtensions.Contains(extension))
                        {
                            // Upload to Supabase Storage
                            var storage = _supabaseClient.Storage.From("user-images");

                            Debug.WriteLine(storage);

                            using var memoryStream = new MemoryStream();
                            await file.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;

                            // Convert MemoryStream to byte[]
                            byte[] fileBytes = memoryStream.ToArray();

                            var uploadResponse = await storage.Upload(fileBytes, newFileName);

                            if (uploadResponse != null)
                            {
                                user.ImagePath = storage.GetPublicUrl(newFileName);
                            }
                            else
                            {
                                return Json(new { success = false, message = "Failed to upload image to storage" });
                            }
                        }
                      
                            else
                            {
                                string errorJson = JsonSerializer.Serialize(new { success = false, message = "FileError Invalid file format. Only JPG, JPEG are allowed." });
                                return new ContentResult
                                {
                                    Content = errorJson,
                                    ContentType = "application/json",
                                    StatusCode = 200
                                };
                            }

                            string json = JsonSerializer.Serialize(user);
                            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                            HttpResponseMessage response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Home/CreateUser", content);
                            string jsonResponse = await response.Content.ReadAsStringAsync();

                            Debug.WriteLine(jsonResponse);

                            if (response.IsSuccessStatusCode)
                            {
                                string successJson = JsonSerializer.Serialize(new { success = true, Message = "User Created Successfully" });
                                return new ContentResult
                                {
                                    Content = successJson,
                                    ContentType = "application/json",
                                    StatusCode = 200
                                };
                            }
                            else
                            {
                                string errorJson = JsonSerializer.Serialize(new { success = false, message = $"API Error: {jsonResponse}" });
                                return new ContentResult
                                {
                                    Content = errorJson,
                                    ContentType = "application/json",
                                    StatusCode = 200
                                };
                            }
                        }
                        else
                        {
                            var errors = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage)
                                .ToList();
                            string errorJson = JsonSerializer.Serialize(new { success = false, message = "FileError Invalid file format. Only JPG, JPEG are allowed." });
                            return new ContentResult
                            {
                                Content = errorJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                    
                   
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> DeleteWebApi(int id)
            {
                try
                {
                    var url = $"{_apiBaseUrl}/api/Home/DeleteUser/{id}";
                    HttpResponseMessage response = await _httpClient.DeleteAsync(url);
                    response.EnsureSuccessStatusCode();
                    var message = await response.Content.ReadAsStringAsync();

                    string successJson = JsonSerializer.Serialize(new { success = true, response = "Deleted Successfully", Name = message });
                    return new ContentResult
                    {
                        Content = successJson,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "No record found: " + ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> EditUserWebApi( UserData editUser, IFormFile file)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        string extension = Path.GetExtension(file.FileName);
                        string newFileName = fileName + "_" + Guid.NewGuid() + extension;

                        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".JPG", ".JPEG" };
                        if (allowedExtensions.Contains(extension))
                        {
                            // Upload to Supabase Storage
                            var storage = _supabaseClient.Storage.From("user-images");

                            Debug.WriteLine(storage);

                            using var memoryStream = new MemoryStream();
                            await file.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;

                            // Convert MemoryStream to byte[]
                            byte[] fileBytes = memoryStream.ToArray();

                            var uploadResponse = await storage.Upload(fileBytes, newFileName);

                            if (uploadResponse != null)
                            {
                                editUser.ImagePath = storage.GetPublicUrl(newFileName);
                            }
                            else
                            {
                                return Json(new { success = false, message = "Failed to upload image to storage" });
                            }
                        }

                      
                        else
                        {
                            ModelState.AddModelError("FileError", "Invalid file format. Only JPG, JPEG are allowed.");
                            string errorJson = JsonSerializer.Serialize(new { success = false, message = "FileError Invalid file format. Only JPG, JPEG are allowed." });
                            return new ContentResult
                            {
                                Content = errorJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                    }
                    else
                    {
                        editUser.ImagePath = "notuploaded";
                    }

                    PopulateSelectLists(editUser);
                    editUser.SelectedCountry = editUser.SelectedCountry ?? "DefaultCountry";
                    editUser.selectedState = editUser.selectedState ?? "DefaultState";
                    editUser.selectedCity = editUser.selectedCity ?? "DefaultCity";
                    editUser.CityList = editUser.CityList ;
                    editUser.StateList = editUser.StateList;
                    editUser.CountryList = editUser.CountryList;

                    string jsonPayload = JsonSerializer.Serialize(editUser);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PutAsync($"{_apiBaseUrl}/api/Home/EditUser", content);
                    string responseMessage = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        string successJson = JsonSerializer.Serialize(new { success = true, message = responseMessage });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        string errorJson = JsonSerializer.Serialize(new { success = false, message = responseMessage });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public IActionResult Edit(int id)
            {
                try
                {
                    var editUser = _applicationDbContext.Users.FirstOrDefault(u => u.user_id == id);

                    if (editUser != null)
                    {
                        PopulateSelectLists(editUser);
                        editUser.ConfirmPassword = editUser.Password;
                        string successJson = JsonSerializer.Serialize(new { success = true, message = editUser });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }

                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "Error occurred" });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            
            }

            [HttpGet]
            public async Task<IActionResult> UserDetailWebApi(int id)
            {
                try
                {
                    HttpResponseMessage message = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/GetbyId/{id}");
                    string jsonResponse = await message.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<UserData>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (message.IsSuccessStatusCode)
                    {
                        string successJson = JsonSerializer.Serialize(new { success = true, message = result });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        string errorJson = JsonSerializer.Serialize(new { success = false, message = jsonResponse });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public async Task<IActionResult> EditUserDetailsWebApi(int id)
            {
                try
                {
                    var url = $"{_apiBaseUrl}/api/Home/GetbyId/{id}";
                    HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var message = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<UserData>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Debug.WriteLine(JsonSerializer.Serialize(result));
                    string successJson = JsonSerializer.Serialize(new { success = true, message = result });
                    return new ContentResult
                    {
                        Content = successJson,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public IActionResult GetAllCountry()
            {
                try
                {
                    var allCountry = _applicationDbContext.Countries.Select(s => new SelectListItem
                    {
                        Value = s.country_id.ToString(),
                        Text = s.CountryName
                    }).ToList();

                    string jsonResult = JsonSerializer.Serialize(allCountry);

                    Debug.WriteLine(jsonResult);

                    return new ContentResult
                    {
                        Content = jsonResult,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public IActionResult GetStatesByCountry(int countryId)
            {
                try
                {
                    var states = _applicationDbContext.states
                        .Where(s => s.country_id == countryId)
                        .Select(s => new SelectListItem
                        {
                            Value = s.state_id.ToString(),
                            Text = s.StateName
                        }).ToList();

                    string jsonResult = JsonSerializer.Serialize(states);
                    return new ContentResult
                    {
                        Content = jsonResult,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public IActionResult GetCityByState(int stateId)
            {
                try
                {
                    var city = _applicationDbContext.Cities
                        .Where(s => s.state_id == stateId)
                        .Select(s => new SelectListItem
                        {
                            Value = s.city_id.ToString(),
                            Text = s.CityName
                        }).ToList();

                    string jsonResult = JsonSerializer.Serialize(city);
                    return new ContentResult
                    {
                        Content = jsonResult,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public IActionResult ViewUser()
            {
                try
                {
                    List<UserData> users = _applicationDbContext.Users.Where(c => c.IsActive == true).ToList();
                    foreach (var user in users)
                    {
                        var city = _applicationDbContext.Cities.FirstOrDefault(c => c.city_id == user.SelectedCityId);
                        user.selectedCity = city != null ? city.CityName : "N/A";

                        var state = city != null ? _applicationDbContext.states.FirstOrDefault(s => s.state_id == city.state_id) : null;
                        user.selectedState = state != null ? state.StateName : "N/A";

                        var country = state != null ? _applicationDbContext.Countries.FirstOrDefault(c => c.country_id == state.country_id) : null;
                        user.SelectedCountry = country != null ? country.CountryName : "N/A";

                        PopulateSelectLists(user);
                    }

                    return View(users);
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }


            [HttpPost]
            public IActionResult ServerData( DataTableRequest dataTableRequest)
            {
                try
                {
                    int draw = dataTableRequest.Draw;
                    int start = dataTableRequest.Start;
                    int length = dataTableRequest.Length;
                    string searchValue = dataTableRequest.Search?.Value?.ToLower() ?? "";

                    // Start with the Users query
                    var query = _applicationDbContext.Users
                        .Where(c => c.IsActive == true)
                        .Join(_applicationDbContext.Cities,
                              user => user.SelectedCityId,
                              city => city.city_id,
                              (user, city) => new { User = user, City = city })
                        .Join(_applicationDbContext.states,
                              uc => uc.City.state_id,
                              state => state.state_id,
                              (uc, state) => new { uc.User, uc.City, State = state })
                        .Join(_applicationDbContext.Countries,
                              ucs => ucs.State.country_id,
                              country => country.country_id,
                              (ucs, country) => new { ucs.User, ucs.City, ucs.State, Country = country });

                    if (!string.IsNullOrEmpty(searchValue))
                    {
                        query = query.Where(ucs => ucs.User.FirstName.ToLower().Contains(searchValue) ||
                                                  ucs.City.CityName.ToLower().Contains(searchValue) ||
                                                  ucs.State.StateName.ToLower().Contains(searchValue) ||
                                                  ucs.Country.CountryName.ToLower().Contains(searchValue) ||
                                                  ucs.User.FirstName.ToLower().Contains(searchValue) ||
                                                  ucs.User.LastName.ToLower().Contains(searchValue) ||
                                                  ucs.User.Email.ToLower().Contains(searchValue) ||
                                                  ucs.User.MobileNo.ToLower().Contains(searchValue) ||
                                                  ucs.User.Gender_.ToLower().Contains(searchValue) ||
                                                  ucs.User.Dob.ToLower().Contains(searchValue) ||
                                                  ucs.User.Address.ToLower().Contains(searchValue));
                    }

                    if (dataTableRequest.Order != null && dataTableRequest.Order.Any())
                    {
                        var order = dataTableRequest.Order[0];

                        switch (order.Column)
                        {
                            case 0: // number
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.user_id) : query.OrderByDescending(ucs => ucs.User.user_id);
                                break;
                            case 1: // FirstName
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.FirstName) : query.OrderByDescending(ucs => ucs.User.FirstName);
                                break;
                            case 2: // LastName
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.LastName) : query.OrderByDescending(ucs => ucs.User.LastName);
                                break;
                            case 3: // Email
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.Email) : query.OrderByDescending(ucs => ucs.User.Email);
                                break;
                            case 4: // MobileNo
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.MobileNo) : query.OrderByDescending(ucs => ucs.User.MobileNo);
                                break;
                            case 5: // Gender
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.Gender_) : query.OrderByDescending(ucs => ucs.User.Gender_);
                                break;
                            case 6: // DOB
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.Dob) : query.OrderByDescending(ucs => ucs.User.Dob);
                                break;
                            case 7: // Address
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.User.Address) : query.OrderByDescending(ucs => ucs.User.Address);
                                break;
                            case 9: // City
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.City.CityName) : query.OrderByDescending(ucs => ucs.City.CityName);
                                break;
                            case 10: // State
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.State.StateName) : query.OrderByDescending(ucs => ucs.State.StateName);
                                break;
                            case 11: // Country
                                query = order.Dir == "asc" ? query.OrderBy(ucs => ucs.Country.CountryName) : query.OrderByDescending(ucs => ucs.Country.CountryName);
                                break;
                            default:
                                query = query.OrderBy(ucs => ucs.User.FirstName); // Default sorting
                                break;
                        }
                    }
                    else
                    {
                        query = query.OrderBy(ucs => ucs.User.user_id);
                    }

                    var totalRecords = _applicationDbContext.Users.Count(c => c.IsActive == true);
                    var filteredRecords = query.Count();
                    var data = query.Skip(start).Take(length).ToList();
                    bool isDescending = (dataTableRequest.Order != null && dataTableRequest.Order.Any() && dataTableRequest.Order[0].Column == 0 && dataTableRequest.Order[0].Dir == "desc");
                    int baseIndex = isDescending ? (filteredRecords - start + 1) : start;
                    int indexIncrement = isDescending ? -1 : 1;

                    var result = new
                    {
                        draw = draw,
                        recordsTotal = totalRecords,
                        recordsFiltered = filteredRecords,
                        data = data.Select((ucs, idx) => new
                        {
                            iid = baseIndex + (idx + 1) * indexIncrement,
                            id = ucs.User.user_id,
                            ucs.User.FirstName,
                            ucs.User.LastName,
                            ucs.User.Email,
                            ucs.User.MobileNo,
                            ucs.User.Gender_,
                            ucs.User.Dob,
                            ucs.User.Address,
                            ucs.User.ImagePath,
                            selectedCity = ucs.City.CityName,
                            selectedState = ucs.State.StateName,
                            SelectedCountry = ucs.Country.CountryName
                        })
                    };

                    string jsonResult = JsonSerializer.Serialize(result);
                    return new ContentResult
                    {
                        Content = jsonResult,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { error = ex.Message });

                    Debug.WriteLine(errorJson);
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> Index( UserData user, IFormFile file)
            {
                try
                {
                   
                        if (file != null && file.Length > 0)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string extension = Path.GetExtension(file.FileName);
                            string newFileName = fileName + "_" + Guid.NewGuid() + extension;
                            string directoryPath = Path.Combine(_env.ContentRootPath, "Uploads");

                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            string filePath = Path.Combine(directoryPath, newFileName);

                            if (extension.ToUpper() == ".JPG" || extension.ToUpper() == ".JPEG" || extension.ToUpper() == ".PNG")
                            {
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                user.ImagePath = newFileName;
                            }
                            else
                            {
                                ModelState.AddModelError("", "Invalid file format");
                                PopulateSelectLists(user);
                                string errorJson = JsonSerializer.Serialize(new { success = false, message = "Invalid file format" });
                                return new ContentResult
                                {
                                    Content = errorJson,
                                    ContentType = "application/json",
                                    StatusCode = 200
                                };
                            }
                      

                        var uniqueEmail = !_applicationDbContext.Users
                            .Any(c => c.Email == user.Email && c.user_id != user.user_id);

                        var uniqueMobileNumber = !_applicationDbContext.Users
                            .Any(c => c.MobileNo == user.MobileNo && c.user_id != user.user_id);

                        if (uniqueEmail && uniqueMobileNumber)
                        {
                            user.IsActive = true;
                            _applicationDbContext.Users.Add(user);
                            _applicationDbContext.SaveChanges();
                            string successJson = JsonSerializer.Serialize(new { success = true, message = "User created successfully!" });
                            return new ContentResult
                            {
                                Content = successJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                        else
                        {
                            string errorJson = JsonSerializer.Serialize(new { success = false, message = "Email or PhoneNumber is already registered" });
                            return new ContentResult
                            {
                                Content = errorJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                    }
                    else
                    {
                        var errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        var errorMessage = "Validation failed." + Environment.NewLine + string.Join(Environment.NewLine, errors);
                        string errorJson = JsonSerializer.Serialize(new { success = false, message = errorMessage });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Invalid file format", ex.Message);
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    var errorMessage = "Validation failed." + Environment.NewLine + string.Join(Environment.NewLine, errors);
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = errorMessage });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> EditUser(UserData user, IFormFile file)
            {
                try
                {
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Invalid user data.");
                        return View();
                    }

                    var editUser = _applicationDbContext.Users.FirstOrDefault(u => u.user_id == user.user_id);
                    if (editUser == null)
                    {
                        return NotFound();
                    }
                    PopulateSelectLists(user);
                    var oldEmail = editUser.Email;
                    var oldPhoneNumber = editUser.MobileNo;

                  
                        if (file != null && file.Length > 0)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string extension = Path.GetExtension(file.FileName);
                            string newFileName = fileName + "_" + Guid.NewGuid() + extension;
                            string directoryPath = Path.Combine(_env.ContentRootPath, "Uploads");

                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            string filePath = Path.Combine(directoryPath, newFileName);
                            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".JPG", ".JPEG" };
                            if (allowedExtensions.Contains(extension))
                            {
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                editUser.ImagePath = newFileName;
                            }
                            else
                            {
                                ModelState.AddModelError("FileError", "Invalid file format. Only JPG, JPEG are allowed.");
                                PopulateSelectLists(user);
                                string errorJson = JsonSerializer.Serialize(new { success = false, message = "FileError Invalid file format. Only JPG, JPEG are allowed." });
                                return new ContentResult
                                {
                                    Content = errorJson,
                                    ContentType = "application/json",
                                    StatusCode = 200
                                };
                            }
                        }

                        editUser.user_id = user.user_id;
                        editUser.FirstName = user.FirstName;
                        editUser.LastName = user.LastName;
                        editUser.Email = user.Email;
                        editUser.MobileNo = user.MobileNo;
                        editUser.Gender1 = user.Gender1;
                        editUser.Dob = user.Dob;
                        editUser.Password = user.Password;
                        editUser.ConfirmPassword = user.ConfirmPassword;
                        editUser.Address = user.Address;
                        editUser.SelectedCountryId = user.SelectedCountryId;
                        editUser.SelectedStateId = user.SelectedStateId;
                        editUser.SelectedCityId = user.SelectedCityId;
                        editUser.IsActive = true;

                        var uniqueEmail = !_applicationDbContext.Users
                            .Any(c => c.Email == editUser.Email && c.user_id != editUser.user_id);

                        var uniqueMobileNumber = !_applicationDbContext.Users
                            .Any(c => c.MobileNo == editUser.MobileNo && c.user_id != editUser.user_id);

                        if (!(uniqueMobileNumber || oldPhoneNumber == editUser.MobileNo))
                        {
                            string errorJson = JsonSerializer.Serialize(new { success = false, message = "Mobile number is already registered with another user!" });
                            return new ContentResult
                            {
                                Content = errorJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                        else if (uniqueEmail || oldEmail == editUser.Email)
                        {
                            _applicationDbContext.SaveChanges();
                            string successJson = JsonSerializer.Serialize(new { success = true, message = "User edited successfully!" });
                            return new ContentResult
                            {
                                Content = successJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                        else
                        {
                            string errorJson = JsonSerializer.Serialize(new { success = false, message = "Email is already registered with another user!" });
                            return new ContentResult
                            {
                                Content = errorJson,
                                ContentType = "application/json",
                                StatusCode = 200
                            };
                        }
                    
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "Some error occurred: " + ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public IActionResult Delete(int id)
            {
                try
                {
                    var deleteUser = _applicationDbContext.Users.FirstOrDefault(u => u.user_id == id);
                    if (deleteUser == null)
                    {
                        string errorJson = JsonSerializer.Serialize(new { success = false, message = "No record found" });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }

                    deleteUser.IsActive = false;
                    _applicationDbContext.SaveChanges();

                    string successJson = JsonSerializer.Serialize(new { success = true, response = "Deleted Successfully", Name = deleteUser.FirstName });
                    return new ContentResult
                    {
                        Content = successJson,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "No record found: " + ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public IActionResult Details(int id)
            {
                try
                {
                    var user = _applicationDbContext.Users.FirstOrDefault(u => u.user_id == id);
                    if (user != null)
                    {
                        var city = _applicationDbContext.Cities.Find(user.SelectedCityId);
                        user.selectedCity = city != null ? city.CityName : "N/A";

                        var state = city != null ? _applicationDbContext.states.Find(city.state_id) : null;
                        user.selectedState = state != null ? state.StateName : "N/A";

                        var country = state != null ? _applicationDbContext.Countries.Find(state.country_id) : null;
                        user.SelectedCountry = country != null ? country.CountryName : "N/A";

                        string successJson = JsonSerializer.Serialize(new { success = true, message = user });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }

                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "Error occurred" });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 200
                    };
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            private void PopulateSelectLists(UserData user)
            {
                try
                {
                    var cityName = _applicationDbContext.Cities.Find(user.SelectedCityId);
                    user.CityList = cityName != null
                        ? _applicationDbContext.Cities
                            .Where(c => c.state_id == cityName.state_id)
                            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                            {
                                Value = c.city_id.ToString(),
                                Text = c.CityName,
                                Selected = c.city_id == user.SelectedCityId
                            }).ToList()
                        : new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

                    var stateName = cityName != null ? _applicationDbContext.states.Find(cityName.state_id) : null;
                    user.StateList = stateName != null
                        ? _applicationDbContext.states
                            .Where(c => c.country_id == stateName.country_id)
                            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                            {
                                Value = c.state_id.ToString(),
                                Text = c.StateName,
                                Selected = c.state_id == cityName.state_id
                            }).ToList()
                        : new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

                    user.SelectedStateId = cityName != null ? cityName.state_id : 0;

                    if (stateName != null)
                    {
                        foreach (var i in user.StateList)
                        {
                            if (i.Selected)
                            {
                                var countryId = _applicationDbContext.Countries.FirstOrDefault(c => c.country_id == stateName.country_id);
                                user.SelectedCountryId = countryId != null ? countryId.country_id : 0;
                            }
                        }
                    }

                    user.CountryList = _applicationDbContext.Countries
                        .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                        {
                            Value = c.country_id.ToString(),
                            Text = c.CountryName,
                            Selected = c.country_id == user.SelectedCountryId
                        }).ToList();
                }
                catch (Exception ex)
                {
                    // Log exception if needed; for now, set empty lists to avoid null issues
                    user.CityList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                    user.StateList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                    user.CountryList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                }
            }

            [HttpGet]
            public IActionResult Contact()
            {
                ViewBag.Message = "Your contact page.";
                return View();
            }

            [HttpGet]
            public IActionResult Index()
            {
                return View();
            }

            [HttpGet]
            public IActionResult Login()
            {
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> UserLogin([FromBody] Login login)
            {


                try
                {
                    string json = JsonSerializer.Serialize(login);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Auth/Login", content);
                    string responseMessage = await response.Content.ReadAsStringAsync();


                    Debug.WriteLine(responseMessage);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {

                        string token = JsonDocument.Parse(responseMessage).RootElement.GetProperty("token").GetString() ?? "N/A";

                        string successJson = JsonSerializer.Serialize(new { success = true, message = token });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };

                    }
                    else 
                    {
                        string successJson = JsonSerializer.Serialize(new { success = false, message = responseMessage });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };

                    }
                   
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = "Some error occurred" });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpPost]
            public async Task<IActionResult> ValidToken( string token)
            {
                try
                {

                    string json = JsonSerializer.Serialize(token);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Auth/validate-token", content);

                    string responsemessage = await response.Content.ReadAsStringAsync();



                    Debug.WriteLine(token);
                    Debug.WriteLine(token);



                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string successJson = JsonSerializer.Serialize(new { success = true, valid = true });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        string errorJson = JsonSerializer.Serialize(new { success = false, valid = false });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception e)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, valid = false });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }

            [HttpGet]
            public async Task<IActionResult> GetDetailsFromToken(string token)
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    HttpResponseMessage result = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Auth/GetDeatilsFromToken");
                    string resultMessage = await result.Content.ReadAsStringAsync();

                    var jsonResult = JsonSerializer.Deserialize<UserData>(resultMessage, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Debug.WriteLine(JsonSerializer.Serialize(jsonResult));

                    if (result.IsSuccessStatusCode)
                    {
                        string successJson = JsonSerializer.Serialize(new { success = true, message = jsonResult });
                        return new ContentResult
                        {
                            Content = successJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        string errorJson = JsonSerializer.Serialize(new { success = false, message = jsonResult });
                        return new ContentResult
                        {
                            Content = errorJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                }
                catch (Exception ex)
                {
                    string errorJson = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                    return new ContentResult
                    {
                        Content = errorJson,
                        ContentType = "application/json",
                        StatusCode = 500
                    };
                }
            }
        }



    }
}
