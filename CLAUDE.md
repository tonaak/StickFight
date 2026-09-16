\# THÔNG TIN DỰ ÁN

\- Tên game: Stick Fight

\- Nền tảng: PC (ưu tiên làm trước) và Android (port sau).

\- Thể loại: 2D Platformer Fighter (Đi cảnh đánh nhau).

\- Phong cách: Hoạt ảnh stickman mượt mà, nhịp độ nhanh, đòn đánh có lực (Hit stop, camera shake).



\# QUY TẮC LẬP TRÌNH UNITY (BẮT BUỘC TUÂN THỦ)

1\. Input: LUÔN LUÔN sử dụng 'New Input System' (UnityEngine.InputSystem), tuyệt đối không dùng Input cũ (Input.GetKey).

2\. Vật lý: Xử lý di chuyển trong `FixedUpdate` bằng `Rigidbody2D`. Không dùng `Translate`.

3\. Tối ưu UI: Mọi UI Canvas đều phải set 'Scale With Screen Size' (1920x1080) để chuẩn bị cho Mobile.

4\. Cấu trúc Code: Viết code module hóa, sử dụng State Machine cho nhân vật và Enemy. Comment tiếng Anh ngắn gọn.

5\. Đa nền tảng: Code logic phải độc lập với Input để sau này dễ dàng gắn nút bấm ảo trên màn hình cảm ứng.

