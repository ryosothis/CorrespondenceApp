   M S B A M L   `   `   `  ����
  Otchet    Otchet.DocumentEditDialog   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Документ��5   	   $	��300��6   $	��400��6'    � WindowStartupLocation$ CenterOwner=�5   	    � 
ResizeMode$ NoResize=�5   	   .��5      � $��10q�6    � RowDefinitions 5	   
   �� 5
      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $��Тип документа��6   $��0��6)   �� 5   
   -   5         ��Name$ txtDocumentType��5      $��1��6+   $��0,0,0,10q�68   �� 5   
   $!��Дата получения��6   $��2��6*   (  "System.Windows.Controls.DatePicker  5   
   -   5          Name$ dpReceivedDate��5      $��3��6-   $��0,5,0,5q�6:   �� 5   
   $#��Дата исполнения��6   $��4��6+     5   
   -   5        $ dpExecutionDate��5      $��4��6.   $��	0,29,0,83q�6;   �� 5   
   $��6��6    �� Orientation$ 
Horizontal=�6"   $��Right=�6;   .+�5      �� -   5        $��Сохранить��5      $��80��6)   $��0,0,10,0q�64    �� 	IsDefault . 6Y   �� 5      -   5        $��Отмена��5      $��80��6&    �� IsCancel . 6F   5      5      5       