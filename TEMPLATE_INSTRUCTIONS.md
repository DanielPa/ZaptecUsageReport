# Excel Template Instructions

## Creating the template.xlsx File

To use the Excel export feature, you need to create a `template.xlsx` file in the project root directory (`ZaptecUsageReport/ZaptecUsageReport/`).

### Template Structure

The template file should have the following columns in row 1 (headers):

| Column | Header | Data Type | Description |
|--------|--------|-----------|-------------|
| A | Start Date/Time | DateTime | When the charging session started |
| B | End Date/Time | DateTime | When the charging session ended |
| C | Duration (Hours) | Number | Duration in decimal hours |
| D | Energy (kWh) | Number | Total energy consumed |
| E | User Name | Text | Full name of the user |
| F | User Email | Text | Email address of the user |
| G | Charger Name | Text | Name of the charger |
| H | Device Name | Text | Device identifier |
| I | Session ID | Text | Unique session identifier |

### Example Template Setup

1. Create a new Excel file named `template.xlsx`
2. In row 1, add the headers listed above
3. **Convert to Excel Table (Recommended)**:
   - Select the header row and at least one data row (e.g., A1:I2)
   - Go to **Insert** → **Table** (or press Ctrl+T / Cmd+T)
   - Make sure "My table has headers" is checked
   - Click OK
   - The table will automatically expand when new rows are added!
4. Format the columns appropriately:
   - Columns A & B: Date/Time format (e.g., `yyyy-MM-dd HH:mm:ss`)
   - Column C: Number format with 2 decimal places
   - Column D: Number format with 2 decimal places
   - Columns E-I: Text format

### Adding Formulas

You can add any Excel formulas to your template. The application will automatically recalculate them after populating the data.

**Example formulas:**

**With Excel Table (Recommended):**
- **Total Energy (in a cell below the table)**:
  `=SUM(Table1[Energy (kWh)])` - Uses structured references, automatically adjusts!

- **Average Session Duration**:
  `=AVERAGE(Table1[Duration (Hours)])`

- **Cost Calculation** (add a new column to the table):
  `=[@[Energy (kWh)]]*0.25` - This formula in column J will auto-fill for all rows

- **Total Row**: You can enable the Table Total Row (Design tab) and it will automatically calculate sums/averages

**Without Table:**
- **Total Energy**:
  `=SUM(D2:D1000)` (adjust range as needed)

- **Average Session Duration**:
  `=AVERAGE(C2:C1000)`

- **Cost Calculation** (in column J):
  `=D2*0.25` (then copy down)

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
