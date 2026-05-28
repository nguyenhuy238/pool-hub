# DOC_SOURCE_MAP - PoolHub

## Source of Truth Priority
1. `swagger_poolhub.yaml` (API contract chính)
2. `poolhub_seed_data_27_tables.sql` + ERD PDF/DOCX (data model và nghiệp vụ DB)
3. Diagram `.svg` / `.html` (context, use case, sequence, sitemap)
4. Proposal/weekly docs `.docx`
5. Code hiện tại trong `pool-hub/` (đang ở mức scaffold)

## Existing Documents Inventory
- API spec: `swagger_poolhub.yaml`
- Database seed + entity hints: `poolhub_seed_data_27_tables.sql`
- ERD and index design:
  - `PoolHub_ERD_27_Bang.pdf`
  - `PoolHub_ERD_27_Bang.docx`
  - `PoolHub_ERD_27_Tables_Diagram_With_Index_Design.pdf`
- Analysis workbook: `Analysis PoolHub.xlsx`
- Context/use case/sitemap/sequence:
  - `poolhub_context_diagram.svg`
  - `poolhub_use_case_diagram.svg`
  - `poolhub_sitemap_ui.svg`
  - `poolhub_seq1_auth.svg`
  - `poolhub_seq2_session.svg`
  - `poolhub_seq3_booking.svg`
  - `poolhub_seq4_invoice.svg`
  - `poolhub_erd_diagram.html`
- Weekly documents:
  - `PoolHub_Proposal_Week1.docx`
  - `PoolHub_Proposal_Week1_New.docx`
  - `PoolHub_Week2_DB_API.docx`
  - `PoolHub_Week3_Backend_Architecture.docx`
  - `PoolHub_Week4_Auth_JWT.docx`

## Notes
- Backend code trong `pool-hub/` hiện chưa triển khai theo đặc tả ở trên.
- Khi phát triển mới, phải đối chiếu `swagger_poolhub.yaml` và SQL/ERD trước khi tạo model/API.
