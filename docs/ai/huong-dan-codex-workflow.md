# Hướng dẫn workflow Codex cho whiteboard-engine

Tài liệu này dùng để:
- Biết ngay dự án đang ở bước nào khi vừa mở Codex.
- Lưu trạng thái đúng cách sau một phiên làm việc để lần sau tiếp tục nhanh.

## 1) Khi bắt đầu mở Codex ở repo này

Mục tiêu: nắm trạng thái hiện tại, step active, và việc tiếp theo.

Thực hiện:
1. Dùng `$resume-project` để đọc:
   - `docs/ai/ai-entry.md`
   - `docs/ai/project-progress.md`
   - `docs/ai/next-task.md`
2. Nếu chỉ cần kiểm tra nhanh đang làm tới đâu, dùng `$tiep-tuc-step`.
3. Xác nhận:
   - `Current Active Step` trong `project-progress.md`
   - Prompt tương ứng trong `docs/ai/prompts/`
   - `next-task.md` có khớp với step active hay không

Kết quả mong đợi:
- Biết chính xác process đang chạy ở step nào.
- Có plan ngắn để tiếp tục đúng scope.

## 2) Trong lúc làm việc

Nguyên tắc:
- Bám `AGENTS.md`.
- Giữ engine-first, deterministic.
- Không mở rộng sang UI/editor.
- Mỗi thay đổi nên nhỏ và dễ review.

## 3) Sau khi làm xong một khoảng thời gian (handoff để lần sau làm tiếp)

Mục tiêu: cập nhật dashboard để phiên sau resume không mất ngữ cảnh.

Thực hiện:
1. Dùng `$capnhat-dashboard`.
2. Cập nhật đúng các file:
   - `docs/ai/project-progress.md`
   - `docs/ai/next-task.md`
   - Lưu prompt step vào `docs/ai/prompts/`
3. Kiểm tra tính nhất quán:
   - Step vừa làm đã vào phần completed (nếu đã xong)
   - Step active mới đúng với kế hoạch tiếp theo
   - `next-task.md` là prompt copy/paste chạy được ngay

## 4) Checklist thực tế trước khi đóng phiên

- [ ] `project-progress.md` phản ánh đúng tình trạng step.
- [ ] `next-task.md` mô tả rõ việc kế tiếp.
- [ ] Prompt step đã được lưu trong `docs/ai/prompts/`.
- [ ] Không có thay đổi ngoài scope không mong muốn.

## 5) Git tối thiểu để lưu trạng thái

```bash
git add .
git commit -m "docs: update AI dashboard and next-task handoff"
git push -u origin main
```

Gợi ý:
- Nếu đang ở giữa step chưa hoàn chỉnh, vẫn nên commit với message rõ trạng thái dở dang để lần sau tiếp tục an toàn.
