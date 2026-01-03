# Excel Template Instructions

## Creating the template.xlsx File

To use the Excel export feature, you need to create a `template.xlsx` file in the project root directory (`ZaptecUsageReport/ZaptecUsageReport/`).

### Template Structure

The template file should have the following columns in row 1 (headers):

| Column | Header | Data Type | Description |
|--------|--------|-----------|-------------|
| A | Session ID | Text | Unique session identifier |
| B | Device ID | Text | Charger device identifier |
| C | Start Date/Time | DateTime | When the charging session started |
| D | End Date/Time | DateTime | When the charging session ended |
| E | Duration | Time | Duration in HH:MM format |
| F | Energy (kWh) | Number | Total energy consumed in kWh |
| G | Signed Session | Text | Cryptographic signature of the session |

### Example Template Setup

1. Create a new Excel file named `template.xlsx`
2. In row 1, add the headers listed above
3. **Convert to Excel Table (Recommended)**:
   - Select the header row and at least one data row (e.g., A1:G2)
   - Go to **Insert** → **Table** (or press Ctrl+T / Cmd+T)
   - Make sure "My table has headers" is checked
   - Click OK
   - The table will automatically expand when new rows are added!
4. Format the columns appropriately:
   - Columns A, B, G: Text format
   - Columns C & D: Date/Time format (e.g., `yyyy-MM-dd HH:mm:ss`)
   - Column E: Time format (e.g., `HH:MM`)
   - Column F: Number format with 2 decimal places

### Adding Formulas

You can add any Excel formulas to your template. The application will automatically recalculate them after populating the data.

**Example formulas:**

**With Excel Table (Recommended):**
- **Total Energy (in a cell below the table)**:
  `=SUM(Table1[Energy (kWh)])` - Uses structured references, automatically adjusts!

- **Session Count**:
  `=COUNTA(Table1[Session ID])`

- **Cost Calculation** (add a new column H to the table):
  `=[@[Energy (kWh)]]*0.25` - This formula will auto-fill for all rows

- **Total Row**: You can enable the Table Total Row (Design tab) and it will automatically calculate sums/averages

**Without Table:**
- **Total Energy**:
  `=SUM(F2:F1000)` (adjust range as needed)

- **Session Count**:
  `=COUNTA(A2:A1000)`

- **Cost Calculation** (in column H):
  `=F2*0.25` (then copy down)

**Conditional Formatting**:
  Highlight rows where energy consumption > 50 kWh (works with both approaches)

### Tips

- **Using Excel Tables is highly recommended** - they automatically expand and use structured references
- The data starts from row 2 (row 1 contains headers)
- Formulas can reference any cells and will be preserved
- Table formulas (structured references like `[@Column]`) will auto-fill to new rows
- You can add multiple sheets to the template
- The export uses the **first worksheet** in the template
- Styling (colors, fonts, borders) from the template will be preserved
- Charts that reference table ranges will automatically update with new data
- Table styles and banded rows are preserved

### Location

Place the `template.xlsx` file at:
```
ZaptecUsageReport/
└── ZaptecUsageReport/
    ├── template.xlsx  ← Place it here
    ├── Program.cs
    ├── appsettings.json
    └── ...
```

The file will be automatically copied to the output directory when you build the project.
