using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace webdulich.Services
{
    // Thông báo kiểm tra dữ liệu bằng tiếng Việt thay cho câu tiếng Anh mặc định của ASP.NET
    // ("The X field is required.", "The field X must be a number."). Áp cho cả máy chủ và phần kiểm tra
    // trên trình duyệt (jquery-validation lấy câu thông báo từ máy chủ qua thuộc tính data-val-*).
    public static class ThongBaoKiemTra
    {
        public const string BatBuoc = "Vui lòng nhập thông tin này.";

        public static void DungTiengViet(this MvcOptions o)
        {
            var tb = o.ModelBindingMessageProvider;
            tb.SetValueMustBeANumberAccessor(_ => "Vui lòng nhập số.");
            tb.SetNonPropertyValueMustBeANumberAccessor(() => "Vui lòng nhập số.");
            tb.SetValueMustNotBeNullAccessor(_ => BatBuoc);
            tb.SetMissingBindRequiredValueAccessor(_ => BatBuoc);
            tb.SetMissingKeyOrValueAccessor(() => BatBuoc);
            tb.SetMissingRequestBodyRequiredValueAccessor(() => BatBuoc);
            tb.SetAttemptedValueIsInvalidAccessor((giaTri, _) => $"Giá trị \"{giaTri}\" không hợp lệ.");
            tb.SetNonPropertyAttemptedValueIsInvalidAccessor(giaTri => $"Giá trị \"{giaTri}\" không hợp lệ.");
            tb.SetUnknownValueIsInvalidAccessor(_ => "Giá trị không hợp lệ.");
            tb.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
            tb.SetValueIsInvalidAccessor(giaTri => $"Giá trị \"{giaTri}\" không hợp lệ.");

            // Thêm sau bộ đọc DataAnnotations để sửa được cả [Required] ngầm định của kiểu không cho phép rỗng
            o.ModelMetadataDetailsProviders.Add(new BatBuocTiengViet());
        }

        // [Required] chưa ghi câu thông báo riêng thì dùng câu tiếng Việt chung
        private sealed class BatBuocTiengViet : IValidationMetadataProvider
        {
            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                bool coBatBuoc = false;
                foreach (var v in context.ValidationMetadata.ValidatorMetadata)
                {
                    if (v is RequiredAttribute r)
                    {
                        coBatBuoc = true;
                        if (string.IsNullOrEmpty(r.ErrorMessage) && r.ErrorMessageResourceName == null)
                        {
                            r.ErrorMessage = BatBuoc;
                        }
                    }
                }

                // Kiểu số, ngày... không cho phép rỗng: ASP.NET tự thêm luật bắt buộc ở bước sau bằng câu tiếng Anh.
                // Thêm sẵn một [Required] tiếng Việt để bước đó dùng luôn (giá trị kiểu này không bao giờ null nên máy chủ không đổi gì).
                var kieu = context.Key.ModelType;
                if (!coBatBuoc && context.Key.MetadataKind == ModelMetadataKind.Property
                    && kieu.IsValueType && Nullable.GetUnderlyingType(kieu) == null)
                {
                    context.ValidationMetadata.ValidatorMetadata.Add(new RequiredAttribute { ErrorMessage = BatBuoc });
                }
            }
        }
    }
}
