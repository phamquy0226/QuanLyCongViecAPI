﻿using System.Text;
using Frontend.Models;
using Newtonsoft.Json;

namespace Frontend.Services
{
    public class WorkItemApiService : IWorkItemApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WorkItemApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]);
        }

        public async Task<List<WorkItemViewModel>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/WorkItem");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("API call failed with status code: " + response.StatusCode);
                    return new List<WorkItemViewModel>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseModel<List<WorkItemViewModel>>>(content);

                if (result != null && result.Success)
                    return result.Data;

                Console.WriteLine("Error: " + (result?.Message ?? "Unknown error"));
                return new List<WorkItemViewModel>();
            }
            catch (Exception ex)
            {
                // Log the error to console or a logging framework
                Console.WriteLine("Error calling GetAllAsync: " + ex.Message);
                return new List<WorkItemViewModel>();
            }
        }

        public async Task<List<WorkItemViewModel>> GetFilteredAsync(WorkItemFilterModel filter)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(filter.UserName)) queryParams.Add($"userName={Uri.EscapeDataString(filter.UserName)}");
            if (!string.IsNullOrEmpty(filter.AssignerName)) queryParams.Add($"assignerName={Uri.EscapeDataString(filter.AssignerName)}");
            if (filter.StartDateFrom.HasValue) queryParams.Add($"startDateFrom={filter.StartDateFrom:yyyy-MM-dd}");
            if (filter.StartDateTo.HasValue) queryParams.Add($"startDateTo={filter.StartDateTo:yyyy-MM-dd}");
            if (filter.EndDateFrom.HasValue) queryParams.Add($"endDateFrom={filter.EndDateFrom:yyyy-MM-dd}");
            if (filter.EndDateTo.HasValue) queryParams.Add($"endDateTo={filter.EndDateTo:yyyy-MM-dd}");
            if (filter.Status.HasValue) queryParams.Add($"status={filter.Status}");
            if (filter.DepartmentID.HasValue) queryParams.Add($"departmentID={filter.DepartmentID}");
            if (!string.IsNullOrEmpty(filter.SearchTaskName)) queryParams.Add($"searchTaskName={Uri.EscapeDataString(filter.SearchTaskName)}");
            if (filter.Priority.HasValue) queryParams.Add($"priority={filter.Priority}");
            if (filter.IsPinned.HasValue) queryParams.Add($"isPinned={filter.IsPinned}");

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/WorkItem?{queryString}");

            if (!response.IsSuccessStatusCode)
                return new List<WorkItemViewModel>();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseModel<List<WorkItemViewModel>>>(content);
            return result?.Data ?? new List<WorkItemViewModel>();
        }


        public async Task<bool> CreateAsync(WorkItemCreateModel model)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(model);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/WorkItem", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to create work item, status code: " + response.StatusCode);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error to console or a logging framework
                Console.WriteLine("Error calling CreateAsync: " + ex.Message);
                return false;
            }
        }

        public async Task<WorkItemDetailModel> GetWorkItemDetailAsync(int id)
        {
            try
            {
                // Thay đổi URL thành /api/WorkItem/{id} (chữ "W" viết hoa)
                var response = await _httpClient.GetAsync($"/api/WorkItem/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<ResponseModel<WorkItemDetailModel>>(content);

                    // Kiểm tra nếu kết quả hợp lệ và trả về dữ liệu
                    if (result != null && result.Success)
                    {
                        return result.Data;
                    }
                    else
                    {
                        Console.WriteLine("Không thể lấy chi tiết cho ID " + id + ": " + (result?.Message ?? "Lỗi không xác định"));
                    }
                }
                else
                {
                    Console.WriteLine("Lỗi khi gọi API, mã trạng thái: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Ghi lại lỗi vào console hoặc framework ghi log
                Console.WriteLine("Lỗi khi gọi GetWorkItemDetailAsync: " + ex.Message);
            }

            return null; // Trả về null nếu không tìm thấy công việc hoặc có lỗi
        }
    }

    public class ResponseModel<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public int? ErrorCode { get; set; }
    }
}
