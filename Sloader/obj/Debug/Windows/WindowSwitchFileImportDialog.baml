   M S B A M L   `   `   `  ����  Sloader*    $Sloader.WindowSwitchFileImportDialog   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Setup Import Data��5   	   $	��350��6#   $	��625��60   .��5      � -   5        $��
gridParent��5       � RowDefinitions 5   
   �� 5      $
��auto �6   6,   �� 5      $��* �6   6)   �� 5	      $
��auto �6   6,   .+�5   
   ]� $��0��6   .+�5      	� -   5        $��	radCs1000��5   %   $��3q�6   .��6L   CS10006T   	� 5      -   5        $��radSl100��5   %   $��3q�6   .��6K   SL100/CS21006Y   	� 5      -   5        $��rad5ess��5   %   $��3q�6   .��6J   Lucent 5ESS6W   	� 5      -   5        $��radEwsd��5   %   $��3q�6   .��6J   EWSD6P   	� 5      -   5        $��	radHipath��5   %   $��3q�6   .��6L   HiPath6T   	� 5      -   5        $��radAvaya��5   %   $��3q�6   .��6K   Avaya6R   �� 5      -   5        $��btnPickFile��5   n   $��0��6   $��Right=�6"   $��3q�69   $	��5,3q�6D   $��Right=�6R   .��6�   File to Import5      5      ]� 5   
   $��1��6   .+�5      �� -	   5        $��tabControlMain��5      .@�5      �� $��Switch Report��6   .��5      &   System.Windows.Controls.DataGrid  -
   5        $��dgSwitchImportData��5      ��69   �� 6S   5      �� 5      -   5        $��tabAssociations��5   0   $��Associations��6   .��5      � -   5        $��	gridAssoc��5       � ColumnDefinitions 5      �� 5      $	��30* �6/   �� 5       $
��581* �6/    5"      �� 5#      $
��auto �6,   6<   �� 5$      $��* �6,   69   .+�5&      �� $��Stretch=�6%   
������ �� Orientation$ 
Horizontal=�6[   $��0��6t   $��2��6�   .+�5'      �� $��0,-3,0,0q�6$   .��66   
EXPORT to:6B   	� 5(      -   5        $��radNewAvaya��5(   *   $��	10,3,10,0q�6=   .��6o   
Avaya CM6y   	� 5)      -   5        $��radNewCisco��5)   *   $��	10,3,10,0q�6=   .��6o   CUCM6u   	� 5*      -   5        $��radNewAs5300��5*   *   $��	10,3,10,0q�6>   .��6p   AS53006x   5+      �� 5,      -   5        $��ScrollViewer1��5,   4   $��1��6'   $
��Auto=�6I   $
��Auto=�6l   ��. $��2��6�   .��5-        -   5        $��dgAssoc��5-   '      AutoGenerateColumns .  $��1��6R   $��2��6_    � 	Resources 5.   "   ��%    50   #      Columns 51   "   0  *System.Windows.Controls.DataGridTextColumn  53   "   ,  &System.Windows.Controls.DataGridColumn   Header$ New Switch Field��65      Width5  /System.Windows.Controls.DataGridLengthConverter$
 Auto 6r   1  +System.Windows.Controls.DataGridBoundColumn	   Binding	 6O   �� *NewSwitchField+6�   4  .System.Windows.Controls.DataGridComboBoxColumn  54   &   $ Old Switch Field��6=   -   5        OldSwitchField��$
 Auto 55   k   
   SelectedValueBinding
 54   p   �� *OldSwitchField+ �� Mode$ TwoWay=�   DisplayMemberPath 55   >   �� *OldSwitchField+56   '     57   &   $ Script��69   	 6I   ��  �� Path$
 Col3�$ TwoWay=� �� ValidatesOnDataErrors . 58   '   5:      5;      5<      5=      5>      5@      �� 5A   
   $��2��6   .��6   Something Button60   5B      5C      